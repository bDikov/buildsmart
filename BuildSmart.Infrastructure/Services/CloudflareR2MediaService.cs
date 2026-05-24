using System.Security.Cryptography;
using System.Text;
using BuildSmart.Core.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BuildSmart.Infrastructure.Services;

public class CloudflareR2MediaService : IMediaService
{
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly string _serviceUrl;
    private readonly string _bucketName;

    public CloudflareR2MediaService(IConfiguration configuration)
    {
        _accessKey = configuration["CloudflareR2:AccessKey"] ?? string.Empty;
        _secretKey = configuration["CloudflareR2:SecretKey"] ?? string.Empty;
        
        // Strip trailing slash if present
        var url = configuration["CloudflareR2:ServiceUrl"] ?? string.Empty;
        _serviceUrl = url.TrimEnd('/');
        
        _bucketName = configuration["CloudflareR2:BucketName"] ?? string.Empty;
    }

    public string GeneratePreSignedUploadUrl(string fileName, string contentType, TimeSpan expiration)
    {
        var method = "PUT";
        var region = "auto";
        var service = "s3";
        var date = DateTime.UtcNow;
        var dateStamp = date.ToString("yyyyMMdd");
        var amzDate = date.ToString("yyyyMMddTHHmmssZ");
        
        // R2 expects the URL format to be: https://<account_id>.r2.cloudflarestorage.com/<bucket_name>/<file_name>
        var canonicalUri = $"/{_bucketName}/{Uri.EscapeDataString(fileName)}";
        
        var credentialScope = $"{dateStamp}/{region}/{service}/aws4_request";
        
        var canonicalHeaders = $"host:{new Uri(_serviceUrl).Host}\nx-amz-date:{amzDate}\n";
        var signedHeaders = "host;x-amz-date";
        
        // Use UNSIGNED-PAYLOAD since we don't know the exact file hash beforehand for a presigned URL
        var payloadHash = "UNSIGNED-PAYLOAD";

        var canonicalRequest = $"{method}\n{canonicalUri}\n\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";

        var algorithm = "AWS4-HMAC-SHA256";
        var stringToSign = $"{algorithm}\n{amzDate}\n{credentialScope}\n{Hash(canonicalRequest)}";

        var signingKey = GetSignatureKey(_secretKey, dateStamp, region, service);
        var signature = ToHexString(HmacSha256(signingKey, stringToSign));

        var presignedUrl = $"{_serviceUrl}{canonicalUri}" +
                           $"?X-Amz-Algorithm={algorithm}" +
                           $"&X-Amz-Credential={Uri.EscapeDataString($"{_accessKey}/{credentialScope}")}" +
                           $"&X-Amz-Date={amzDate}" +
                           $"&X-Amz-Expires={(int)expiration.TotalSeconds}" +
                           $"&X-Amz-SignedHeaders={signedHeaders}" +
                           $"&X-Amz-Signature={signature}";

        return presignedUrl;
    }

    private static byte[] HmacSha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static byte[] GetSignatureKey(string key, string dateStamp, string regionName, string serviceName)
    {
        var kSecret = Encoding.UTF8.GetBytes("AWS4" + key);
        var kDate = HmacSha256(kSecret, dateStamp);
        var kRegion = HmacSha256(kDate, regionName);
        var kService = HmacSha256(kRegion, serviceName);
        return HmacSha256(kService, "aws4_request");
    }

    private static string Hash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return ToHexString(bytes);
    }

    private static string ToHexString(byte[] bytes)
    {
        var hex = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            hex.AppendFormat("{0:x2}", b);
        }
        return hex.ToString();
    }
}