using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IUnifiedMediaService
{
    // Folder Management
    Task<List<MediaFolder>> GetFoldersAsync(Guid? parentId = null, CancellationToken ct = default);
    Task<MediaFolder?> GetFolderByIdAsync(Guid folderId, CancellationToken ct = default);
    Task<MediaFolder?> GetFolderByPathAsync(string fullPath, CancellationToken ct = default);
    Task<MediaFolder> CreateFolderAsync(string name, Guid? parentId = null, CancellationToken ct = default);
    Task<MediaFolder> RenameFolderAsync(Guid folderId, string newName, CancellationToken ct = default);
    Task<bool> DeleteFolderAsync(Guid folderId, CancellationToken ct = default);
    Task<MediaFolder> EnsureFolderPathAsync(string folderPath, CancellationToken ct = default);

    // Direct Upload & Presigned URLs
    Task<string> GenerateFolderPresignedUploadUrlAsync(string folderPath, string fileName, string contentType, TimeSpan? expiration = null);
    Task<MediaAsset> RegisterUploadedAssetAsync(
        Guid? folderId,
        string r2Key,
        string fileName,
        string contentType,
        long sizeBytes,
        int? width = null,
        int? height = null,
        double? durationSeconds = null,
        Guid? uploaderUserId = null,
        CancellationToken ct = default);

    Task<MediaAsset> UploadAndOptimizeImageAsync(
        Stream stream,
        string fileName,
        string contentType,
        Guid? folderId,
        Guid? uploaderUserId = null,
        CancellationToken ct = default);

    // Asset Operations
    Task<bool> DeleteAssetAsync(Guid assetId, CancellationToken ct = default);
    Task<MediaAsset> UpdateAssetMetadataAsync(Guid assetId, string? altTextBg, string? altTextEn, string? fileName, Guid? folderId, CancellationToken ct = default);
    Task<MediaAsset> MoveAssetAsync(Guid assetId, Guid? targetFolderId, CancellationToken ct = default);

    // Queries
    Task<(List<MediaAsset> Items, int TotalCount)> GetAssetsAsync(
        Guid? folderId,
        string? mediaType,
        string? searchTerm,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default);
}
