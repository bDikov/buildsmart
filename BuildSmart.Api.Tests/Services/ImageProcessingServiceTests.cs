using System.IO;
using System.Threading.Tasks;
using BuildSmart.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace BuildSmart.Api.Tests.Services;

public class ImageProcessingServiceTests
{
    [Fact]
    public void IsSupportedImage_ReturnsExpectedResults()
    {
        var service = new ImageProcessingService(NullLogger<ImageProcessingService>.Instance);

        Assert.True(service.IsSupportedImage("photo.jpg", "image/jpeg"));
        Assert.True(service.IsSupportedImage("picture.png", "image/png"));
        Assert.True(service.IsSupportedImage("banner.webp", "image/webp"));
        Assert.True(service.IsSupportedImage("scan.bmp", "image/bmp"));
        Assert.False(service.IsSupportedImage("video.mp4", "video/mp4"));
        Assert.False(service.IsSupportedImage("doc.pdf", "application/pdf"));
    }

    [Fact]
    public async Task ProcessImageAsync_ResizesAndConvertsToWebp()
    {
        var service = new ImageProcessingService(NullLogger<ImageProcessingService>.Instance);

        // Create a synthetic 2400x1200 image in memory
        using var testImage = new Image<Rgba32>(2400, 1200);
        using var inputStream = new MemoryStream();
        await testImage.SaveAsPngAsync(inputStream);
        inputStream.Position = 0;

        await using var result = await service.ProcessImageAsync(inputStream, "Luxury Living Room.png", maxFullDimension: 1920, maxThumbDimension: 360);

        Assert.NotNull(result);
        Assert.Equal(2400, result.Width);
        Assert.Equal(1200, result.Height);
        Assert.Equal("image/webp", result.ContentType);
        Assert.Equal("Luxury_Living_Room.webp", result.WebpFileName);
        Assert.True(result.FullSizeBytes > 0);
        Assert.True(result.ThumbSizeBytes > 0);

        // Verify the processed stream can be read back and inspected
        result.FullImageStream.Position = 0;
        using var loadedFull = await Image.LoadAsync(result.FullImageStream);
        Assert.Equal(1920, loadedFull.Width);
        Assert.Equal(960, loadedFull.Height); // 2400:1200 aspect ratio preserved!

        result.ThumbnailStream.Position = 0;
        using var loadedThumb = await Image.LoadAsync(result.ThumbnailStream);
        Assert.Equal(360, loadedThumb.Width);
        Assert.Equal(180, loadedThumb.Height);
    }
}
