using System.Security.Cryptography;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using BuildSmart.Core.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BuildSmart.Infrastructure.Services;

public class CloudflareR2MediaService : IMediaService
{
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly string _serviceUrl;
    private readonly string _bucketName;
    private readonly IAmazonS3 _s3Client;
    private readonly IConfiguration _configuration;

    public CloudflareR2MediaService(IConfiguration configuration)
    {
        _configuration = configuration;
        _accessKey = configuration["CloudflareR2:AccessKey"] ?? string.Empty;
        _secretKey = configuration["CloudflareR2:SecretKey"] ?? string.Empty;
        
        _bucketName = configuration["CloudflareR2:BucketName"] ?? string.Empty;

        // Strip trailing slash if present
        var url = configuration["CloudflareR2:ServiceUrl"] ?? string.Empty;
        url = url.TrimEnd('/');
        
        // CRITICAL FIX: Cloudflare dashboard provides the S3 URL *with* the bucket name appended.
        // If the user pasted that into the environment variable, the AWS SDK (which uses Path-Style) 
        // will append the bucket name a SECOND time, causing a 404 NotFound. 
        // We must strip the bucket name from the Service URL if it exists.
        if (url.EndsWith($"/{_bucketName}", StringComparison.OrdinalIgnoreCase))
        {
            url = url.Substring(0, url.Length - $"/{_bucketName}".Length);
        }
        
        _serviceUrl = url;
        
        var s3Config = new AmazonS3Config
        {
            ServiceURL = _serviceUrl,
            ForcePathStyle = true // CRITICAL: Cloudflare R2 requires Path-Style URLs
        };

        _s3Client = new AmazonS3Client(_accessKey, _secretKey, s3Config);
    }

    public string GeneratePreSignedUploadUrl(string fileName, string contentType, TimeSpan expiration)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = fileName,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiration),
            Protocol = Protocol.HTTPS
        };

        var url = _s3Client.GetPreSignedURL(request);
        return url;
    }

    public async Task<string> UploadFileAsync(Stream stream, string fileName, string contentType)
    {
        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = fileName,
            InputStream = stream,
            ContentType = contentType,
            DisablePayloadSigning = true, // CRITICAL: Cloudflare R2 does not support streaming signature payloads
            UseChunkEncoding = false // CRITICAL: Cloudflare R2 does not support streaming signature payloads (payload trailer)
        };

        await _s3Client.PutObjectAsync(putRequest);

        var publicBaseUrl = _configuration["CloudflareR2:PublicUrl"];
        if (!string.IsNullOrEmpty(publicBaseUrl))
        {
            return $"{publicBaseUrl.TrimEnd('/')}/{fileName}";
        }

        return $"{_serviceUrl.TrimEnd('/')}/{_bucketName}/{fileName}";
    }

    public async Task DeleteFileAsync(string fileUrl)
    {
        var fileName = System.IO.Path.GetFileName(fileUrl);
        if (string.IsNullOrEmpty(fileName)) return;

        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = fileName
        };

        await _s3Client.DeleteObjectAsync(deleteRequest);
    }
}