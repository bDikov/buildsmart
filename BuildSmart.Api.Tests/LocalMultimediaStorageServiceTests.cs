using BuildSmart.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace BuildSmart.Api.Tests;

public class LocalMultimediaStorageServiceTests
{
    [Fact]
    public async Task SaveFileAsync_ValidFile_ReturnsRelativePath()
    {
        // Arrange
        var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempFolder);

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.SetupGet(c => c["WebRootPath"]).Returns(tempFolder);

        var mockLogger = new Mock<ILogger<LocalMultimediaStorageService>>();
        var service = new LocalMultimediaStorageService(mockConfig.Object, mockLogger.Object);

        var fileContent = "dummy image data"u8.ToArray();
        using var stream = new MemoryStream(fileContent);

        // Act
        var resultUrl = await service.SaveFileAsync(stream, "test-image.png", "image/png");

        // Assert
        Assert.NotNull(resultUrl);
        Assert.StartsWith("/uploads/", resultUrl);
        Assert.EndsWith("test-image.png", resultUrl);

        var expectedFilePath = Path.Combine(tempFolder, resultUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(expectedFilePath));

        // Cleanup
        Directory.Delete(tempFolder, true);
    }

    [Fact]
    public async Task DeleteFileAsync_ExistingFile_DeletesFile()
    {
        // Arrange
        var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var uploadsFolder = Path.Combine(tempFolder, "uploads");
        Directory.CreateDirectory(uploadsFolder);

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.SetupGet(c => c["WebRootPath"]).Returns(tempFolder);

        var mockLogger = new Mock<ILogger<LocalMultimediaStorageService>>();
        var service = new LocalMultimediaStorageService(mockConfig.Object, mockLogger.Object);

        var testFileName = "test-delete.png";
        var fileUrl = $"/uploads/{testFileName}";
        var absolutePath = Path.Combine(uploadsFolder, testFileName);
        
        await File.WriteAllTextAsync(absolutePath, "dummy content");
        Assert.True(File.Exists(absolutePath));

        // Act
        await service.DeleteFileAsync(fileUrl);

        // Assert
        Assert.False(File.Exists(absolutePath));

        // Cleanup
        Directory.Delete(tempFolder, true);
    }
}
