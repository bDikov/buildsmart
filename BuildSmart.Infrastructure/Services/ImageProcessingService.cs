using System;
using System.IO;
using System.Threading.Tasks;
using BuildSmart.Core.Application.Interfaces;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace BuildSmart.Infrastructure.Services;

public class ImageProcessingService : IImageProcessingService
{
    private readonly ILogger<ImageProcessingService> _logger;

    public ImageProcessingService(ILogger<ImageProcessingService> logger)
    {
        _logger = logger;
    }

    public bool IsSupportedImage(string fileName, string contentType)
    {
        if (!string.IsNullOrWhiteSpace(contentType) && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            if (contentType.Contains("svg") || contentType.Contains("gif")) return false;
            return true;
        }

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp" or ".tiff" or ".tif" or ".heic";
    }

    public async Task<ProcessedImageResult> ProcessImageAsync(
        Stream inputStream,
        string originalFileName,
        int maxFullDimension = 1920,
        int maxThumbDimension = 360,
        int quality = 85)
    {
        if (inputStream == null || inputStream.Length == 0)
        {
            throw new ArgumentException("Input image stream is empty.", nameof(inputStream));
        }

        if (inputStream.CanSeek && inputStream.Position > 0)
        {
            inputStream.Position = 0;
        }

        using var image = await Image.LoadAsync(inputStream);

        // Auto-orient based on EXIF
        image.Mutate(x => x.AutoOrient());

        var origWidth = image.Width;
        var origHeight = image.Height;

        var fullMs = new MemoryStream();
        var thumbMs = new MemoryStream();

        var webpEncoder = new WebpEncoder
        {
            Quality = Math.Clamp(quality, 1, 100)
        };

        // 1. Process Full High-Res Version
        using (var fullClone = image.Clone(ctx =>
        {
            if (origWidth > maxFullDimension || origHeight > maxFullDimension)
            {
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(maxFullDimension, maxFullDimension),
                    Mode = ResizeMode.Max
                });
            }
        }))
        {
            await fullClone.SaveAsync(fullMs, webpEncoder);
        }

        // 2. Process Thumbnail Version
        using (var thumbClone = image.Clone(ctx =>
        {
            ctx.Resize(new ResizeOptions
            {
                Size = new Size(maxThumbDimension, maxThumbDimension),
                Mode = ResizeMode.Max
            });
        }))
        {
            await thumbClone.SaveAsync(thumbMs, webpEncoder);
        }

        fullMs.Position = 0;
        thumbMs.Position = 0;

        var nameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
        var cleanBaseName = System.Text.RegularExpressions.Regex.Replace(nameWithoutExt, @"[^a-zA-Z0-9_\-]", "_");
        if (string.IsNullOrWhiteSpace(cleanBaseName)) cleanBaseName = "media";

        var result = new ProcessedImageResult
        {
            FullImageStream = fullMs,
            ThumbnailStream = thumbMs,
            Width = origWidth,
            Height = origHeight,
            FullSizeBytes = fullMs.Length,
            ThumbSizeBytes = thumbMs.Length,
            ContentType = "image/webp",
            WebpFileName = $"{cleanBaseName}.webp"
        };

        _logger.LogInformation("Processed image {FileName}: Orig {W}x{H}, FullWebP: {FullBytes} bytes, Thumb: {ThumbBytes} bytes",
            originalFileName, origWidth, origHeight, result.FullSizeBytes, result.ThumbSizeBytes);

        return result;
    }
}
