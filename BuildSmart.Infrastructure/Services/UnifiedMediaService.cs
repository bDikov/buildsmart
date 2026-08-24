using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BuildSmart.Infrastructure.Services;

public class UnifiedMediaService : IUnifiedMediaService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UnifiedMediaService> _logger;
    private readonly IImageProcessingService _imageProcessingService;
    private readonly IAmazonS3? _s3Client;
    private readonly string _bucketName;
    private readonly string _publicBaseUrl;
    private readonly string _serviceUrl;

    public UnifiedMediaService(
        AppDbContext context,
        IConfiguration configuration,
        ILogger<UnifiedMediaService> logger,
        IImageProcessingService imageProcessingService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _imageProcessingService = imageProcessingService;

        var accessKey = configuration["CloudflareR2:AccessKey"] ?? string.Empty;
        var secretKey = configuration["CloudflareR2:SecretKey"] ?? string.Empty;
        _bucketName = configuration["CloudflareR2:BucketName"] ?? string.Empty;
        _publicBaseUrl = configuration["CloudflareR2:PublicUrl"] ?? string.Empty;

        var url = configuration["CloudflareR2:ServiceUrl"] ?? string.Empty;
        url = url.TrimEnd('/');
        if (!string.IsNullOrEmpty(_bucketName) && url.EndsWith($"/{_bucketName}", StringComparison.OrdinalIgnoreCase))
        {
            url = url.Substring(0, url.Length - $"/{_bucketName}".Length);
        }
        _serviceUrl = url;

        if (!string.IsNullOrWhiteSpace(_serviceUrl) && !string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey))
        {
            var s3Config = new AmazonS3Config
            {
                ServiceURL = _serviceUrl,
                ForcePathStyle = true
            };
            _s3Client = new AmazonS3Client(accessKey, secretKey, s3Config);
        }
    }

    #region Folder Management

    public async Task<List<MediaFolder>> GetFoldersAsync(Guid? parentId = null, CancellationToken ct = default)
    {
        var query = _context.MediaFolders
            .Include(f => f.SubFolders)
            .Include(f => f.Assets)
            .AsNoTracking();

        if (parentId.HasValue)
        {
            query = query.Where(f => f.ParentId == parentId.Value);
        }
        else
        {
            query = query.Where(f => f.ParentId == null);
        }

        return await query.OrderBy(f => f.Name).ToListAsync(ct);
    }

    public async Task<MediaFolder?> GetFolderByIdAsync(Guid folderId, CancellationToken ct = default)
    {
        return await _context.MediaFolders
            .Include(f => f.SubFolders)
            .Include(f => f.Assets)
            .FirstOrDefaultAsync(f => f.Id == folderId, ct);
    }

    public async Task<MediaFolder?> GetFolderByPathAsync(string fullPath, CancellationToken ct = default)
    {
        var cleanPath = NormalizePath(fullPath);
        return await _context.MediaFolders
            .Include(f => f.SubFolders)
            .Include(f => f.Assets)
            .FirstOrDefaultAsync(f => f.FullPath == cleanPath, ct);
    }

    public async Task<MediaFolder> CreateFolderAsync(string name, Guid? parentId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Folder name cannot be empty.", nameof(name));

        var cleanName = name.Trim();
        var slug = Slugify(cleanName);

        string parentPath = string.Empty;
        if (parentId.HasValue)
        {
            var parent = await _context.MediaFolders.FindAsync(new object[] { parentId.Value }, ct);
            if (parent == null)
                throw new InvalidOperationException($"Parent folder {parentId.Value} not found.");
            parentPath = parent.FullPath.TrimEnd('/');
        }

        var fullPath = $"{parentPath}/{slug}";

        // Ensure unique slug within same parent
        var existing = await _context.MediaFolders
            .FirstOrDefaultAsync(f => f.ParentId == parentId && f.Slug == slug, ct);
        if (existing != null)
        {
            return existing;
        }

        var folder = new MediaFolder
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Name = cleanName,
            Slug = slug,
            FullPath = fullPath,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.MediaFolders.AddAsync(folder, ct);
        await _context.SaveChangesAsync(ct);

        return folder;
    }

    public async Task<MediaFolder> RenameFolderAsync(Guid folderId, string newName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("New folder name cannot be empty.", nameof(newName));

        var folder = await _context.MediaFolders.FindAsync(new object[] { folderId }, ct);
        if (folder == null)
            throw new InvalidOperationException($"Folder {folderId} not found.");

        if (folder.IsSystem)
            throw new InvalidOperationException("System folders cannot be renamed.");

        var cleanName = newName.Trim();
        var newSlug = Slugify(cleanName);
        var oldPath = folder.FullPath;

        string parentPath = string.Empty;
        if (folder.ParentId.HasValue)
        {
            var parent = await _context.MediaFolders.FindAsync(new object[] { folder.ParentId.Value }, ct);
            if (parent != null) parentPath = parent.FullPath.TrimEnd('/');
        }

        var newPath = $"{parentPath}/{newSlug}";
        folder.Name = cleanName;
        folder.Slug = newSlug;
        folder.FullPath = newPath;
        folder.UpdatedAt = DateTime.UtcNow;

        // Cascade update descendant folder FullPaths
        var allFolders = await _context.MediaFolders.ToListAsync(ct);
        UpdateDescendantPaths(folder.Id, newPath, allFolders);

        await _context.SaveChangesAsync(ct);
        return folder;
    }

    public async Task<bool> DeleteFolderAsync(Guid folderId, CancellationToken ct = default)
    {
        var folder = await _context.MediaFolders
            .Include(f => f.Assets)
            .Include(f => f.SubFolders)
            .FirstOrDefaultAsync(f => f.Id == folderId, ct);

        if (folder == null) return false;

        if (folder.IsSystem)
            throw new InvalidOperationException("System folders cannot be deleted.");

        // Recursively find and delete all assets from R2
        var allAssetsToDelete = new List<MediaAsset>();
        await CollectAssetsRecursively(folderId, allAssetsToDelete, ct);

        foreach (var asset in allAssetsToDelete)
        {
            try
            {
                await DeleteR2ObjectAsync(asset.R2Key);
                if (!string.IsNullOrEmpty(asset.ThumbnailUrl) && asset.ThumbnailUrl != asset.PublicUrl)
                {
                    var thumbKey = ExtractKeyFromUrl(asset.ThumbnailUrl);
                    if (!string.IsNullOrEmpty(thumbKey)) await DeleteR2ObjectAsync(thumbKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete asset {R2Key} from R2 during folder deletion.", asset.R2Key);
            }
        }

        _context.MediaFolders.Remove(folder);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<MediaFolder> EnsureFolderPathAsync(string folderPath, CancellationToken ct = default)
    {
        var normalized = NormalizePath(folderPath);
        if (string.IsNullOrEmpty(normalized) || normalized == "/")
        {
            var general = await _context.MediaFolders.FirstOrDefaultAsync(f => f.Slug == "general" && f.ParentId == null, ct);
            if (general != null) return general;
            return await CreateFolderAsync("General", null, ct);
        }

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        Guid? currentParentId = null;
        MediaFolder? currentFolder = null;

        foreach (var segment in segments)
        {
            var slug = Slugify(segment);
            currentFolder = await _context.MediaFolders
                .FirstOrDefaultAsync(f => f.ParentId == currentParentId && f.Slug == slug, ct);

            if (currentFolder == null)
            {
                var name = ToTitleCase(segment.Replace('-', ' '));
                currentFolder = await CreateFolderAsync(name, currentParentId, ct);
            }

            currentParentId = currentFolder.Id;
        }

        return currentFolder!;
    }

    #endregion

    #region Direct Upload & Presigned URLs

    public Task<string> GenerateFolderPresignedUploadUrlAsync(string folderPath, string fileName, string contentType, TimeSpan? expiration = null)
    {
        if (_s3Client == null)
            throw new InvalidOperationException("Cloudflare R2 is not configured.");

        var cleanFolder = NormalizePath(folderPath).Trim('/');
        var cleanFileName = SanitizeFileName(fileName);
        var uniqueFileName = $"{Guid.NewGuid():N}_{cleanFileName}";
        var r2Key = string.IsNullOrEmpty(cleanFolder) ? uniqueFileName : $"{cleanFolder}/{uniqueFileName}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = r2Key,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromHours(1)),
            Protocol = Protocol.HTTPS
        };

        var presignedUrl = _s3Client.GetPreSignedURL(request);
        return Task.FromResult(presignedUrl);
    }

    public async Task<MediaAsset> RegisterUploadedAssetAsync(
        Guid? folderId,
        string r2Key,
        string fileName,
        string contentType,
        long sizeBytes,
        int? width = null,
        int? height = null,
        double? durationSeconds = null,
        Guid? uploaderUserId = null,
        CancellationToken ct = default)
    {
        var cleanKey = r2Key.TrimStart('/');
        if (!string.IsNullOrEmpty(_bucketName) && cleanKey.StartsWith($"{_bucketName}/", StringComparison.OrdinalIgnoreCase))
        {
            cleanKey = cleanKey.Substring($"{_bucketName}/".Length).TrimStart('/');
        }

        var publicUrl = BuildPublicUrl(cleanKey);
        var mediaType = DetermineMediaType(contentType, fileName);

        // Check if existing asset with same key exists
        var existing = await _context.MediaAssets.FirstOrDefaultAsync(a => a.R2Key == cleanKey, ct);
        if (existing != null)
        {
            existing.FileName = fileName;
            existing.ContentType = contentType;
            existing.SizeBytes = sizeBytes;
            if (width.HasValue) existing.Width = width;
            if (height.HasValue) existing.Height = height;
            if (durationSeconds.HasValue) existing.DurationSeconds = durationSeconds;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return existing;
        }

        // If folderId is not supplied, attempt to resolve folder from R2 key prefix
        if (!folderId.HasValue)
        {
            var keyFolderPart = Path.GetDirectoryName(cleanKey)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(keyFolderPart))
            {
                var folder = await EnsureFolderPathAsync(keyFolderPart, ct);
                folderId = folder.Id;
            }
        }

        var asset = new MediaAsset
        {
            Id = Guid.NewGuid(),
            FolderId = folderId,
            FileName = fileName,
            R2Key = cleanKey,
            PublicUrl = publicUrl,
            ThumbnailUrl = mediaType == "image" ? publicUrl : null,
            MediaType = mediaType,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            Width = width,
            Height = height,
            DurationSeconds = durationSeconds,
            UploaderUserId = uploaderUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.MediaAssets.AddAsync(asset, ct);
        await _context.SaveChangesAsync(ct);

        return asset;
    }

    public async Task<MediaAsset> UploadAndOptimizeImageAsync(
        Stream stream,
        string fileName,
        string contentType,
        Guid? folderId,
        Guid? uploaderUserId = null,
        CancellationToken ct = default)
    {
        if (_s3Client == null)
            throw new InvalidOperationException("Cloudflare R2 is not configured.");

        // Check folder path
        string folderPrefix = "general";
        if (folderId.HasValue)
        {
            var folder = await _context.MediaFolders.FindAsync(new object[] { folderId.Value }, ct);
            if (folder != null)
            {
                folderPrefix = folder.FullPath.Trim('/');
            }
        }

        var assetId = Guid.NewGuid();

        if (_imageProcessingService.IsSupportedImage(fileName, contentType))
        {
            await using var processed = await _imageProcessingService.ProcessImageAsync(stream, fileName);

            var mainKey = $"{folderPrefix}/{assetId:N}_{processed.WebpFileName}";
            var thumbKey = $"{folderPrefix}/thumb_{assetId:N}_{processed.WebpFileName}";

            // Upload Main WebP
            await PutR2StreamAsync(mainKey, processed.FullImageStream, processed.ContentType);

            // Upload Thumb WebP
            await PutR2StreamAsync(thumbKey, processed.ThumbnailStream, processed.ContentType);

            var mainUrl = BuildPublicUrl(mainKey);
            var thumbUrl = BuildPublicUrl(thumbKey);

            var asset = new MediaAsset
            {
                Id = assetId,
                FolderId = folderId,
                FileName = processed.WebpFileName,
                R2Key = mainKey,
                PublicUrl = mainUrl,
                ThumbnailUrl = thumbUrl,
                MediaType = "image",
                ContentType = processed.ContentType,
                SizeBytes = processed.FullSizeBytes,
                Width = processed.Width,
                Height = processed.Height,
                UploaderUserId = uploaderUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.MediaAssets.AddAsync(asset, ct);
            await _context.SaveChangesAsync(ct);
            return asset;
        }
        else
        {
            // Non-processable image or raw document
            var cleanFileName = SanitizeFileName(fileName);
            var r2Key = $"{folderPrefix}/{assetId:N}_{cleanFileName}";

            if (stream.CanSeek && stream.Position > 0) stream.Position = 0;
            await PutR2StreamAsync(r2Key, stream, contentType);

            var publicUrl = BuildPublicUrl(r2Key);
            var mediaType = DetermineMediaType(contentType, fileName);

            var asset = new MediaAsset
            {
                Id = assetId,
                FolderId = folderId,
                FileName = cleanFileName,
                R2Key = r2Key,
                PublicUrl = publicUrl,
                ThumbnailUrl = mediaType == "image" ? publicUrl : null,
                MediaType = mediaType,
                ContentType = contentType,
                SizeBytes = stream.Length,
                UploaderUserId = uploaderUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.MediaAssets.AddAsync(asset, ct);
            await _context.SaveChangesAsync(ct);
            return asset;
        }
    }

    #endregion

    #region Asset Operations

    public async Task<bool> DeleteAssetAsync(Guid assetId, CancellationToken ct = default)
    {
        var asset = await _context.MediaAssets.FindAsync(new object[] { assetId }, ct);
        if (asset != null)
        {
            try
            {
                await DeleteR2ObjectAsync(asset.R2Key);

                if (!string.IsNullOrEmpty(asset.ThumbnailUrl) && asset.ThumbnailUrl != asset.PublicUrl)
                {
                    var thumbKey = ExtractKeyFromUrl(asset.ThumbnailUrl);
                    if (!string.IsNullOrEmpty(thumbKey))
                    {
                        await DeleteR2ObjectAsync(thumbKey);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete R2 objects for asset {AssetId}", assetId);
            }

            _context.MediaAssets.Remove(asset);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        // If not in MediaAssets, check legacy TradesmanMedia
        var tm = await _context.TradesmanMedia.FindAsync(new object[] { assetId }, ct);
        if (tm != null)
        {
            _context.TradesmanMedia.Remove(tm);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        return true;
    }

    public async Task<MediaAsset> UpdateAssetMetadataAsync(
        Guid assetId,
        string? altTextBg,
        string? altTextEn,
        string? fileName,
        Guid? folderId,
        CancellationToken ct = default)
    {
        var asset = await _context.MediaAssets.FindAsync(new object[] { assetId }, ct);
        if (asset == null)
            throw new InvalidOperationException($"Asset {assetId} not found.");

        if (!string.IsNullOrWhiteSpace(altTextBg)) asset.AltTextBg = altTextBg;
        if (!string.IsNullOrWhiteSpace(altTextEn)) asset.AltTextEn = altTextEn;
        if (!string.IsNullOrWhiteSpace(fileName)) asset.FileName = fileName;
        if (folderId.HasValue) asset.FolderId = folderId.Value;

        asset.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return asset;
    }

    public async Task<MediaAsset> MoveAssetAsync(Guid assetId, Guid? targetFolderId, CancellationToken ct = default)
    {
        var asset = await _context.MediaAssets.FindAsync(new object[] { assetId }, ct);
        if (asset == null)
            throw new InvalidOperationException($"Asset {assetId} not found.");

        asset.FolderId = targetFolderId;
        asset.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return asset;
    }

    public async Task<(List<MediaAsset> Items, int TotalCount)> GetAssetsAsync(
        Guid? folderId,
        string? mediaType,
        string? searchTerm,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default)
    {
        var query = _context.MediaAssets.AsNoTracking();

        if (folderId.HasValue)
        {
            var folder = await _context.MediaFolders.FindAsync(new object[] { folderId.Value }, ct);
            if (folder != null)
            {
                var targetPrefix = folder.FullPath.ToLower();
                var matchingFolderIds = await _context.MediaFolders
                    .Where(f => f.Id == folderId.Value || f.FullPath.ToLower().StartsWith(targetPrefix + "/"))
                    .Select(f => f.Id)
                    .ToListAsync(ct);

                query = query.Where(a => a.FolderId.HasValue && matchingFolderIds.Contains(a.FolderId.Value));
            }
            else
            {
                query = query.Where(a => a.FolderId == folderId.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(mediaType) && !string.Equals(mediaType, "all", StringComparison.OrdinalIgnoreCase))
        {
            var cleanType = mediaType.ToLowerInvariant().Trim();
            query = query.Where(a => a.MediaType == cleanType);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(a =>
                a.FileName.ToLower().Contains(term) ||
                (a.AltTextBg != null && a.AltTextBg.ToLower().Contains(term)) ||
                (a.AltTextEn != null && a.AltTextEn.ToLower().Contains(term)) ||
                a.PublicUrl.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    #endregion

    #region Helpers

    private async Task PutR2StreamAsync(string r2Key, Stream stream, string contentType)
    {
        if (_s3Client == null) throw new InvalidOperationException("R2 client not initialized.");

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = r2Key,
            InputStream = stream,
            ContentType = contentType,
            DisablePayloadSigning = true,
            UseChunkEncoding = false
        };

        await _s3Client.PutObjectAsync(putRequest);
    }

    private async Task DeleteR2ObjectAsync(string r2Key)
    {
        if (_s3Client == null || string.IsNullOrWhiteSpace(r2Key)) return;

        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = r2Key
        };

        await _s3Client.DeleteObjectAsync(deleteRequest);
    }

    private string BuildPublicUrl(string r2Key)
    {
        var cleanKey = r2Key.TrimStart('/');
        if (!string.IsNullOrEmpty(_bucketName) && cleanKey.StartsWith($"{_bucketName}/", StringComparison.OrdinalIgnoreCase))
        {
            cleanKey = cleanKey.Substring($"{_bucketName}/".Length).TrimStart('/');
        }

        if (!string.IsNullOrEmpty(_publicBaseUrl))
        {
            return $"{_publicBaseUrl.TrimEnd('/')}/{cleanKey}";
        }

        return $"{_serviceUrl.TrimEnd('/')}/{_bucketName}/{cleanKey}";
    }

    private string? ExtractKeyFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var decoded = System.Net.WebUtility.UrlDecode(url);
        if (!string.IsNullOrEmpty(_publicBaseUrl) && decoded.StartsWith(_publicBaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            return decoded.Substring(_publicBaseUrl.Length).TrimStart('/');
        }

        var uri = new Uri(url);
        return uri.AbsolutePath.TrimStart('/');
    }

    private static string DetermineMediaType(string contentType, string fileName)
    {
        if (!string.IsNullOrEmpty(contentType))
        {
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return "image";
            if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)) return "video";
            if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)) return "audio";
        }

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" or ".bmp" or ".svg" or ".avif" => "image",
            ".mp4" or ".webm" or ".mov" or ".mkv" or ".avi" => "video",
            _ => "document"
        };
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "/";
        var cleaned = path.Trim().Replace('\\', '/');
        if (!cleaned.StartsWith('/')) cleaned = "/" + cleaned;
        return cleaned.TrimEnd('/');
    }

    private static string Slugify(string text)
    {
        var s = text.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"\s+", "-");
        s = Regex.Replace(s, @"[^a-z0-9а-яё\-]", "");
        s = Regex.Replace(s, @"\-+", "-");
        return s.Trim('-');
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        return Regex.Replace(name, @"[^a-zA-Z0-9_\.\-]", "_");
    }

    private static string ToTitleCase(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
    }

    private void UpdateDescendantPaths(Guid parentId, string parentPath, List<MediaFolder> allFolders)
    {
        var children = allFolders.Where(f => f.ParentId == parentId).ToList();
        foreach (var child in children)
        {
            var childPath = $"{parentPath}/{child.Slug}";
            child.FullPath = childPath;
            child.UpdatedAt = DateTime.UtcNow;
            UpdateDescendantPaths(child.Id, childPath, allFolders);
        }
    }

    private async Task CollectAssetsRecursively(Guid folderId, List<MediaAsset> result, CancellationToken ct)
    {
        var assets = await _context.MediaAssets.Where(a => a.FolderId == folderId).ToListAsync(ct);
        result.AddRange(assets);

        var subFolderIds = await _context.MediaFolders.Where(f => f.ParentId == folderId).Select(f => f.Id).ToListAsync(ct);
        foreach (var subId in subFolderIds)
        {
            await CollectAssetsRecursively(subId, result, ct);
        }
    }

    #endregion
}
