namespace BuildSmart.Core.Application.Interfaces;

public interface IMediaService
{
    /// <summary>
    /// Generates a pre-signed URL for direct upload to CDN (Cloudflare R2/S3).
    /// </summary>
    string GeneratePreSignedUploadUrl(string fileName, string contentType, TimeSpan expiration);

    /// <summary>
    /// Uploads a file stream directly to the CDN and returns its public URL.
    /// </summary>
    Task<string> UploadFileAsync(Stream stream, string fileName, string contentType);

    /// <summary>
    /// Deletes a file from the CDN using its public URL.
    /// </summary>
    Task DeleteFileAsync(string fileUrl);
}