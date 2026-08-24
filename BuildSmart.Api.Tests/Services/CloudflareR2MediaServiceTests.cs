using System;
using BuildSmart.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace BuildSmart.Api.Tests.Services;

public class CloudflareR2MediaServiceTests
{
    [Fact]
    public void Constructor_StripsBucketNameFromServiceUrl_PreventsDuplicatePathInUrl()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        
        var bucketName = "buildsmart-media";
        var accountUrl = "https://f1e77b3a2be1bd259203245ad13c667d.r2.cloudflarestorage.com";
        // Simulate the user pasting the URL exactly as provided by the Cloudflare Dashboard (with bucket name appended)
        var dirtyServiceUrl = $"{accountUrl}/{bucketName}";
        
        mockConfig.Setup(c => c["CloudflareR2:AccessKey"]).Returns("dummy-access-key");
        mockConfig.Setup(c => c["CloudflareR2:SecretKey"]).Returns("dummy-secret-key");
        mockConfig.Setup(c => c["CloudflareR2:BucketName"]).Returns(bucketName);
        mockConfig.Setup(c => c["CloudflareR2:ServiceUrl"]).Returns(dirtyServiceUrl);

        var mediaService = new CloudflareR2MediaService(mockConfig.Object);

        // Act
        var fileName = $"{Guid.NewGuid()}_test-video.mp4";
        var preSignedUrl = mediaService.GeneratePreSignedUploadUrl(fileName, "video/mp4", TimeSpan.FromMinutes(15));

        // Assert
        // The URL MUST start with the raw account URL, followed by EXACTLY ONE bucket name, followed by the file name.
        // It should NOT contain ".../buildsmart-media/buildsmart-media/..."
        
        var expectedPathSegment = $"/{bucketName}/{fileName}";
        var invalidDuplicatePathSegment = $"/{bucketName}/{bucketName}/";
        
        Assert.StartsWith(accountUrl, preSignedUrl);
        Assert.Contains(expectedPathSegment, preSignedUrl);
        Assert.DoesNotContain(invalidDuplicatePathSegment, preSignedUrl);
    }
}