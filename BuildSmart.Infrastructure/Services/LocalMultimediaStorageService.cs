using BuildSmart.Core.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace BuildSmart.Infrastructure.Services;

public class LocalMultimediaStorageService : IMultimediaStorageService
{
    private readonly string _uploadsFolder;
    private readonly ILogger<LocalMultimediaStorageService> _logger;

    public LocalMultimediaStorageService(IConfiguration configuration, ILogger<LocalMultimediaStorageService> logger)
    {
        _logger = logger;
        // Read from config or default to a local "uploads" folder
        var webRoot = configuration["WebRootPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        _uploadsFolder = Path.Combine(webRoot, "uploads");
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType)
    {
        if (fileStream == null || fileStream.Length == 0)
            throw new ArgumentException("File stream is empty.", nameof(fileStream));
        
        if (!Directory.Exists(_uploadsFolder))
        {
            Directory.CreateDirectory(_uploadsFolder);
        }

        // Generate a unique file name to prevent overwriting
        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(_uploadsFolder, uniqueFileName);

        using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileStreamOutput);
        }

        _logger.LogInformation("Saved file to {FilePath}", filePath);

        // Return a relative URL path (e.g., "/uploads/123-abc.png")
        return $"/uploads/{uniqueFileName}";
    }

    public Task DeleteFileAsync(string fileUrl)
    {
        try
        {
            // fileUrl should be like "/uploads/123-abc.png"
            var relativePath = fileUrl.TrimStart('/');
            var absolutePath = Path.Combine(Directory.GetParent(_uploadsFolder)?.FullName ?? Directory.GetCurrentDirectory(), relativePath);

            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
                _logger.LogInformation("Deleted file at {FilePath}", absolutePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file at {FileUrl}", fileUrl);
            // Non-critical failure; we might just have orphaned files.
        }

        return Task.CompletedTask;
    }
}
