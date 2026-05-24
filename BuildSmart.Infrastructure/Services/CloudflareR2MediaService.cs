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

    public CloudflareR2MediaService(IConfiguration configuration)
    {
        _accessKey = configuration["CloudflareR2:AccessKey"] ?? string.Empty;
        _secretKey = configuration["CloudflareR2:SecretKey"] ?? string.Empty;
        
        // Strip trailing slash if present
        var url = configuration["CloudflareR2:ServiceUrl"] ?? string.Empty;
        _serviceUrl = url.TrimEnd('/');
        
        _bucketName = configuration["CloudflareR2:BucketName"] ?? string.Empty;
        
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
}