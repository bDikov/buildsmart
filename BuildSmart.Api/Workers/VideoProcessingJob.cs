using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Path = System.IO.Path;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BuildSmart.Api.Workers;

public class VideoProcessingJob
{
    private readonly AppDbContext _context;
    private readonly IMediaService _mediaService;
    private readonly IConfiguration _config;
    private readonly ILogger<VideoProcessingJob> _logger;

    public VideoProcessingJob(
        AppDbContext context,
        IMediaService mediaService,
        IConfiguration config,
        ILogger<VideoProcessingJob> logger)
    {
        _context = context;
        _mediaService = mediaService;
        _config = config;
        _logger = logger;
    }

    public async Task ProcessVideoAsync(Guid mediaId)
    {
        _logger.LogInformation("Starting video processing job for TradesmanMedia: {MediaId}", mediaId);

        var media = await _context.TradesmanMedia.FindAsync(mediaId);
        if (media == null)
        {
            _logger.LogWarning("TradesmanMedia record {MediaId} not found.", mediaId);
            return;
        }

        if (string.IsNullOrEmpty(media.VideoUrl))
        {
            _logger.LogWarning("TradesmanMedia record {MediaId} has no VideoUrl.", mediaId);
            return;
        }

        await ProcessVideoUrlInternalAsync(
            originalRawUrl: media.VideoUrl,
            existingThumbnailUrl: media.ImageUrl,
            folderKeyPrefix: $"feed/{media.TradesmanId}",
            onSuccess: async (desktopUrl, mobileUrl, posterUrl) =>
            {
                var oldVideoUrl = media.VideoUrl;
                media.VideoUrl = desktopUrl;
                media.MobileVideoUrl = mobileUrl;
                media.ImageUrl = posterUrl;
                media.ThumbnailUrl = posterUrl;
                media.UpdatedAt = DateTime.UtcNow;

                var matchingAsset = await _context.MediaAssets.FirstOrDefaultAsync(a => a.PublicUrl == oldVideoUrl || a.PublicUrl == desktopUrl);
                if (matchingAsset != null)
                {
                    matchingAsset.PublicUrl = desktopUrl;
                    matchingAsset.ThumbnailUrl = posterUrl;
                    matchingAsset.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            });
    }

    public async Task ProcessMediaAssetVideoAsync(Guid assetId)
    {
        _logger.LogInformation("Starting video processing job for MediaAsset: {AssetId}", assetId);

        var asset = await _context.MediaAssets
            .Include(a => a.Folder)
            .FirstOrDefaultAsync(a => a.Id == assetId);

        if (asset == null)
        {
            _logger.LogWarning("MediaAsset record {AssetId} not found.", assetId);
            return;
        }

        if (string.IsNullOrEmpty(asset.PublicUrl))
        {
            _logger.LogWarning("MediaAsset record {AssetId} has no PublicUrl.", assetId);
            return;
        }

        var folderPrefix = asset.Folder != null ? asset.Folder.FullPath.Trim('/') : "general";

        await ProcessVideoUrlInternalAsync(
            originalRawUrl: asset.PublicUrl,
            existingThumbnailUrl: asset.ThumbnailUrl,
            folderKeyPrefix: folderPrefix,
            onSuccess: async (desktopUrl, mobileUrl, posterUrl) =>
            {
                asset.PublicUrl = desktopUrl;
                asset.ThumbnailUrl = posterUrl;
                asset.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            });
    }

    private async Task ProcessVideoUrlInternalAsync(
        string originalRawUrl,
        string? existingThumbnailUrl,
        string folderKeyPrefix,
        Func<string, string, string?, Task> onSuccess)
    {
        await EnsureFfmpegBinaryAsync();

        var tempRoot = Path.GetTempPath();
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
        {
            if (tempRoot.Contains('\\') || tempRoot.Contains(':'))
            {
                tempRoot = "/tmp";
            }
        }

        var tempDir = Path.Combine(tempRoot, "buildsmart_video_temp", Guid.NewGuid().ToString());
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }

        var originalVideoPath = Path.Combine(tempDir, "input.mp4");
        var mobileVideoPath = Path.Combine(tempDir, "mobile.mp4");
        var desktopVideoPath = Path.Combine(tempDir, "desktop.mp4");
        var posterImagePath = Path.Combine(tempDir, "poster.jpg");

        try
        {
            // 1. Download original video
            _logger.LogInformation("Downloading original video from {Url} to {Path}", originalRawUrl, originalVideoPath);
            using (var httpClient = new HttpClient())
            {
                using var response = await httpClient.GetAsync(originalRawUrl);
                response.EnsureSuccessStatusCode();
                using var fileStream = File.Create(originalVideoPath);
                await response.Content.CopyToAsync(fileStream);
            }

            var ffmpegExe = GetFfmpegPath();

            // 2. Compress video for mobile (720p)
            _logger.LogInformation("Compressing video for mobile 720p...");
            var compressArgs = $"-i \"{originalVideoPath}\" -vcodec libx264 -crf 28 -preset fast -filter:v \"scale=720:-2\" -acodec aac -b:a 128k -movflags +faststart -y \"{mobileVideoPath}\"";
            await RunProcessAsync(ffmpegExe, compressArgs);

            // 2.5 Compress video for desktop (1080p web-optimized)
            _logger.LogInformation("Compressing video for desktop 1080p...");
            var desktopCompressArgs = $"-i \"{originalVideoPath}\" -vcodec libx264 -crf 23 -preset fast -filter:v \"scale='-2:trunc(min(1080,ih)/2)*2'\" -acodec aac -b:a 192k -movflags +faststart -y \"{desktopVideoPath}\"";
            await RunProcessAsync(ffmpegExe, desktopCompressArgs);

            // 3. Extract cover thumbnail (if not already uploaded)
            var generatedThumbnail = false;
            if (string.IsNullOrEmpty(existingThumbnailUrl))
            {
                _logger.LogInformation("Extracting cover thumbnail frame at 1.0s...");
                var thumbnailArgs = $"-i \"{originalVideoPath}\" -ss 00:00:01 -vframes 1 -f image2 -y \"{posterImagePath}\"";
                await RunProcessAsync(ffmpegExe, thumbnailArgs);
                generatedThumbnail = true;
            }

            var decodedUrl = System.Net.WebUtility.UrlDecode(originalRawUrl);
            var rawFileName = Path.GetFileName(decodedUrl);
            var cleanFileName = rawFileName;
            if (cleanFileName.StartsWith("desktop_")) cleanFileName = cleanFileName.Substring("desktop_".Length);
            else if (cleanFileName.StartsWith("mobile_")) cleanFileName = cleanFileName.Substring("mobile_".Length);

            var fileGuid = Guid.NewGuid().ToString("N");
            var cleanPrefix = folderKeyPrefix.Trim('/');

            // 4. Upload mobile video to R2
            string mobileVideoUrl;
            using (var mobileStream = File.OpenRead(mobileVideoPath))
            {
                var mobileKey = string.IsNullOrEmpty(cleanPrefix) ? $"mobile_{fileGuid}_{cleanFileName}" : $"{cleanPrefix}/mobile_{fileGuid}_{cleanFileName}";
                _logger.LogInformation("Uploading mobile video to R2: {Key}", mobileKey);
                mobileVideoUrl = await _mediaService.UploadFileAsync(mobileStream, mobileKey, "video/mp4");
            }

            // 4.5 Upload desktop video to R2
            string desktopVideoUrl;
            using (var desktopStream = File.OpenRead(desktopVideoPath))
            {
                var desktopKey = string.IsNullOrEmpty(cleanPrefix) ? $"desktop_{fileGuid}_{cleanFileName}" : $"{cleanPrefix}/desktop_{fileGuid}_{cleanFileName}";
                _logger.LogInformation("Uploading desktop video to R2: {Key}", desktopKey);
                desktopVideoUrl = await _mediaService.UploadFileAsync(desktopStream, desktopKey, "video/mp4");
            }

            // 5. Upload thumbnail image to R2 (if generated)
            string? posterImageUrl = existingThumbnailUrl;
            if (generatedThumbnail && File.Exists(posterImagePath))
            {
                using (var imgStream = File.OpenRead(posterImagePath))
                {
                    var posterKey = string.IsNullOrEmpty(cleanPrefix) ? $"poster_{fileGuid}.jpg" : $"{cleanPrefix}/poster_{fileGuid}.jpg";
                    _logger.LogInformation("Uploading extracted poster to R2: {Key}", posterKey);
                    posterImageUrl = await _mediaService.UploadFileAsync(imgStream, posterKey, "image/jpeg");
                }
            }

            // 6. Callback for database update
            await onSuccess(desktopVideoUrl, mobileVideoUrl, posterImageUrl);
            _logger.LogInformation("Video processing completed successfully. Desktop: {Desktop}, Mobile: {Mobile}, Poster: {Poster}", desktopVideoUrl, mobileVideoUrl, posterImageUrl);

            // 7. Delete original raw video from R2 (only if it was not already a processed file)
            var originalFileName = Path.GetFileName(decodedUrl).ToLower();
            if (!originalFileName.StartsWith("desktop_") && !originalFileName.StartsWith("mobile_"))
            {
                try
                {
                    _logger.LogInformation("Deleting original raw video {Url} from CDN...", originalRawUrl);
                    await _mediaService.DeleteFileAsync(originalRawUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete original raw video {Url} from CDN.", originalRawUrl);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process video from URL: {Url}", originalRawUrl);
            throw;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up temp directory {Path}", tempDir);
            }
        }
    }

    private async Task EnsureFfmpegBinaryAsync()
    {
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
        {
            return;
        }

        var ffmpegDir = @"C:\Users\bonch\source\repos\BuildSmart\scratch\ffmpeg";
        var ffmpegExePath = Path.Combine(ffmpegDir, "ffmpeg.exe");

        if (File.Exists(ffmpegExePath))
        {
            return;
        }

        _logger.LogInformation("ffmpeg not found. Downloading static build for local Windows environment...");

        if (!Directory.Exists(ffmpegDir))
        {
            Directory.CreateDirectory(ffmpegDir);
        }

        var downloadUrl = "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffmpeg-6.1-win-64.zip";
        var zipPath = Path.Combine(ffmpegDir, "ffmpeg.zip");

        try
        {
            using (var httpClient = new HttpClient())
            {
                var data = await httpClient.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(zipPath, data);
            }

            _logger.LogInformation("Extracting ffmpeg...");
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, ffmpegDir);
            File.Delete(zipPath);
            _logger.LogInformation("ffmpeg successfully installed to {Path}", ffmpegExePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download/extract ffmpeg for local Windows development.");
            throw;
        }
    }

    private string GetFfmpegPath()
    {
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
        {
            return "ffmpeg";
        }

        var scratchPath = @"C:\Users\bonch\source\repos\BuildSmart\scratch\ffmpeg\ffmpeg.exe";
        if (File.Exists(scratchPath))
        {
            return scratchPath;
        }

        return "ffmpeg";
    }

    private async Task RunProcessAsync(string filename, string arguments)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = filename,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new System.Diagnostics.Process { StartInfo = startInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            _logger.LogError("Process failed with exit code {Code}. Output: {Out}. Error: {Err}", process.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
            throw new Exception($"Process execution failed: {filename} {arguments}");
        }
    }
}
