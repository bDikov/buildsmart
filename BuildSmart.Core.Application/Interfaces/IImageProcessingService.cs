using System;
using System.IO;
using System.Threading.Tasks;

namespace BuildSmart.Core.Application.Interfaces;

public class ProcessedImageResult : IAsyncDisposable, IDisposable
{
    public Stream FullImageStream { get; set; } = Stream.Null;
    public Stream ThumbnailStream { get; set; } = Stream.Null;
    public int Width { get; set; }
    public int Height { get; set; }
    public long FullSizeBytes { get; set; }
    public long ThumbSizeBytes { get; set; }
    public string ContentType { get; set; } = "image/webp";
    public string WebpFileName { get; set; } = string.Empty;

    public void Dispose()
    {
        FullImageStream?.Dispose();
        ThumbnailStream?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (FullImageStream != null) await FullImageStream.DisposeAsync();
        if (ThumbnailStream != null) await ThumbnailStream.DisposeAsync();
    }
}

public interface IImageProcessingService
{
    /// <summary>
    /// Checks if a content type or file extension is a supported image format.
    /// </summary>
    bool IsSupportedImage(string fileName, string contentType);

    /// <summary>
    /// Processes an input image: auto-orients from EXIF, converts to WebP, creates full and thumbnail streams.
    /// </summary>
    Task<ProcessedImageResult> ProcessImageAsync(
        Stream inputStream,
        string originalFileName,
        int maxFullDimension = 1920,
        int maxThumbDimension = 360,
        int quality = 85);
}
