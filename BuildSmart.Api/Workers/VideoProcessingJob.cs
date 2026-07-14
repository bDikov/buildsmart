using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
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
        _logger.LogInformation("Starting video processing job for media: {MediaId}", mediaId);

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

        await EnsureFfmpegBinaryAsync();

        // Create a temporary workspace folder in the system temp directory
        var tempRoot = System.IO.Path.GetTempPath();
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
        {
            if (tempRoot.Contains('\\') || tempRoot.Contains(':'))
            {
                tempRoot = "/tmp";
            }
        }

        var tempDir = System.IO.Path.Combine(tempRoot, "buildsmart_video_temp", Guid.NewGuid().ToString());
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }

        var originalVideoPath = System.IO.Path.Combine(tempDir, "input.mp4");
        var mobileVideoPath = System.IO.Path.Combine(tempDir, "mobile.mp4");
        var desktopVideoPath = System.IO.Path.Combine(tempDir, "desktop.mp4");
        var posterImagePath = System.IO.Path.Combine(tempDir, "poster.jpg");

        var originalRawUrl = media.VideoUrl;

        try
        {
            // 1. Download original video
            _logger.LogInformation("Downloading original video from {Url} to {Path}", originalRawUrl, originalVideoPath);
            using (var httpClient = new HttpClient())
            {
                using var response = await httpClient.GetAsync(originalRawUrl);
                response.EnsureSuccessStatusCode();
                using var fileStream = System.IO.File.Create(originalVideoPath);
                await response.Content.CopyToAsync(fileStream);
            }

            var ffmpegExe = GetFfmpegPath();

            // 2. Compress video for mobile (720p)
            _logger.LogInformation("Compressing video for mobile...");
            var compressArgs = $"-i \"{originalVideoPath}\" -vcodec libx264 -crf 28 -preset fast -filter:v \"scale=720:-2\" -acodec aac -b:a 128k -movflags +faststart -y \"{mobileVideoPath}\"";
            await RunProcessAsync(ffmpegExe, compressArgs);

            // 2.5 Compress video for desktop (1080p web-optimized)
            _logger.LogInformation("Compressing video for desktop...");
            var desktopCompressArgs = $"-i \"{originalVideoPath}\" -vcodec libx264 -crf 23 -preset fast -filter:v \"scale='-2:trunc(min(1080,ih)/2)*2'\" -acodec aac -b:a 192k -movflags +faststart -y \"{desktopVideoPath}\"";
            await RunProcessAsync(ffmpegExe, desktopCompressArgs);

            // 3. Extract cover thumbnail (if not already uploaded)
            var generatedThumbnail = false;
            if (string.IsNullOrEmpty(media.ImageUrl))
            {
                _logger.LogInformation("Extracting cover thumbnail frame...");
                // Extract 1 frame at 1.0 second offset
                var thumbnailArgs = $"-i \"{originalVideoPath}\" -ss 00:00:01 -vframes 1 -f image2 -y \"{posterImagePath}\"";
                await RunProcessAsync(ffmpegExe, thumbnailArgs);
                generatedThumbnail = true;
            }

            // Extract filename and strip existing prefixes to prevent double-prefixing on re-queue
            var rawFileName = System.IO.Path.GetFileName(originalRawUrl);
            var cleanFileName = rawFileName;
            if (cleanFileName.StartsWith("desktop_"))
            {
                cleanFileName = cleanFileName.Substring("desktop_".Length);
            }
            else if (cleanFileName.StartsWith("mobile_"))
            {
                cleanFileName = cleanFileName.Substring("mobile_".Length);
            }

            // 4. Upload mobile video to R2
            string mobileVideoUrl;
            using (var mobileStream = System.IO.File.OpenRead(mobileVideoPath))
            {
                var mobileFileName = $"mobile_{mediaId}_{cleanFileName}";
                _logger.LogInformation("Uploading mobile video to CDN...");
                mobileVideoUrl = await _mediaService.UploadFileAsync(mobileStream, mobileFileName, "video/mp4");
            }

            // 4.5 Upload desktop video to R2
            string desktopVideoUrl;
            using (var desktopStream = System.IO.File.OpenRead(desktopVideoPath))
            {
                var desktopFileName = $"desktop_{mediaId}_{cleanFileName}";
                _logger.LogInformation("Uploading desktop video to CDN...");
                desktopVideoUrl = await _mediaService.UploadFileAsync(desktopStream, desktopFileName, "video/mp4");
            }

            // 5. Upload thumbnail image to R2 (if generated)
            string? posterImageUrl = media.ImageUrl;
            if (generatedThumbnail && File.Exists(posterImagePath))
            {
                using (var imgStream = System.IO.File.OpenRead(posterImagePath))
                {
                    var posterFileName = $"poster_{mediaId}.jpg";
                    _logger.LogInformation("Uploading extracted poster to CDN...");
                    posterImageUrl = await _mediaService.UploadFileAsync(imgStream, posterFileName, "image/jpeg");
                }
            }

            // 6. Update database record
            media.VideoUrl = desktopVideoUrl;
            media.MobileVideoUrl = mobileVideoUrl;
            media.ImageUrl = posterImageUrl;
            media.ThumbnailUrl = posterImageUrl;
            media.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Video processing job complete. Desktop URL: {DesktopUrl}, Mobile URL: {MobileUrl}, Poster URL: {PosterUrl}", desktopVideoUrl, mobileVideoUrl, posterImageUrl);

            // 7. Delete original raw video from R2 (only if it was not already a compressed version)
            if (!originalRawUrl.Contains("/desktop_") && !originalRawUrl.Contains("/mobile_"))
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
            _logger.LogError(ex, "Failed to process video for media: {MediaId}", mediaId);
            throw; // Rethrow to let Hangfire retry if needed
        }
        finally
        {
            // Clean up temp files
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
        var ffmpegExePath = System.IO.Path.Combine(ffmpegDir, "ffmpeg.exe");

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
        var zipPath = System.IO.Path.Combine(ffmpegDir, "ffmpeg.zip");

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
