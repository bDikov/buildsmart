namespace BuildSmart.Core.Application.Interfaces;

public interface IMediaService
{
    /// <summary>
    /// Generates a pre-signed URL for direct upload to CDN (Cloudflare R2/S3).
    /// </summary>
    string GeneratePreSignedUploadUrl(string fileName, string contentType, TimeSpan expiration);
}