namespace BuildSmart.Core.Application.Interfaces;

public interface IMultimediaStorageService
{
    /// <summary>
    /// Saves a file stream and returns the URL or path to access it.
    /// </summary>
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType);

    /// <summary>
    /// Deletes a file by its URL or path.
    /// </summary>
    Task DeleteFileAsync(string fileUrl);
}
