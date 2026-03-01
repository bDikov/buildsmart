using System.Net.Http.Headers;
using BuildSmart.Maui.Services;

namespace BuildSmart.Maui.Services;

public interface IFileService
{
    Task<string?> UploadPortfolioEntryAsync(string title, string? description, Stream fileStream, string fileName);
    Task<string?> UploadCertificationAsync(string title, string? description, DateTime issuedAt, DateTime? expiresAt, Stream fileStream, string fileName);
    Task<string?> UpdateVideoIntroductionAsync(Stream fileStream, string fileName);
}

public class FileService : IFileService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;

    public FileService(HttpClient httpClient, IAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    public async Task<string?> UploadPortfolioEntryAsync(string title, string? description, Stream fileStream, string fileName)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(title), "title");
        if (description != null) content.Add(new StringContent(description), "description");
        
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));
        content.Add(streamContent, "file", fileName);

        await AddAuthHeader();
        var response = await _httpClient.PostAsync($"{ApiConfig.GetBaseUrl()}/api/upload/portfolio", content);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();
            // In a real app, parse JSON to get the URL. For now, just return success indicator.
            return "Success"; 
        }
        return null;
    }

    public async Task<string?> UploadCertificationAsync(string title, string? description, DateTime issuedAt, DateTime? expiresAt, Stream fileStream, string fileName)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(title), "title");
        if (description != null) content.Add(new StringContent(description), "description");
        content.Add(new StringContent(issuedAt.ToString("O")), "issuedAt");
        if (expiresAt.HasValue) content.Add(new StringContent(expiresAt.Value.ToString("O")), "expiresAt");

        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));
        content.Add(streamContent, "file", fileName);

        await AddAuthHeader();
        var response = await _httpClient.PostAsync($"{ApiConfig.GetBaseUrl()}/api/upload/certification", content);

        if (response.IsSuccessStatusCode) return "Success";
        return null;
    }

    public async Task<string?> UpdateVideoIntroductionAsync(Stream fileStream, string fileName)
    {
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));
        content.Add(streamContent, "file", fileName);

        await AddAuthHeader();
        var response = await _httpClient.PostAsync($"{ApiConfig.GetBaseUrl()}/api/upload/video-intro", content);

        if (response.IsSuccessStatusCode) return "Success";
        return null;
    }

    private async Task AddAuthHeader()
    {
        var token = await _authService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            ".mp4" => "video/mp4",
            _ => "application/octet-stream",
        };
    }
}
