using System;
using System.Collections.Generic;

namespace BuildSmart.Core.Application.DTOs;

public class MediaFolderDto
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public int ItemCount { get; set; }
    public int SubFolderCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class MediaAssetDto
{
    public Guid Id { get; set; }
    public Guid? FolderId { get; set; }
    public string? FolderPath { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string R2Key { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string MediaType { get; set; } = "image";
    public string ContentType { get; set; } = "image/webp";
    public long SizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? DurationSeconds { get; set; }
    public string? AltTextBg { get; set; }
    public string? AltTextEn { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Backward-compat helpers
    public string Url { get => PublicUrl; set => PublicUrl = value; }
    public string Type { get => MediaType; set => MediaType = value; }
    public string Title { get => FileName; set => FileName = value; }
}

public class MediaAssetsResultDto
{
    public int TotalCount { get; set; }
    public List<MediaAssetDto> Items { get; set; } = new();
}

public class CreateMediaFolderInput
{
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
}

public class UpdateMediaAssetInput
{
    public Guid Id { get; set; }
    public string? AltTextBg { get; set; }
    public string? AltTextEn { get; set; }
    public string? FileName { get; set; }
    public Guid? FolderId { get; set; }
}
