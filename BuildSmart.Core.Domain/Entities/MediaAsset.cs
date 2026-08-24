using System;

namespace BuildSmart.Core.Domain.Entities;

/// <summary>
/// Represents an optimized media asset stored in Cloudflare R2.
/// </summary>
public class MediaAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? FolderId { get; set; }
    public MediaFolder? Folder { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string R2Key { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }

    public string MediaType { get; set; } = "image"; // 'image' | 'video' | 'document'
    public string ContentType { get; set; } = "image/webp";
    public long SizeBytes { get; set; }

    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? DurationSeconds { get; set; }

    public string? AltTextBg { get; set; }
    public string? AltTextEn { get; set; }

    public Guid? UploaderUserId { get; set; }
    public User? UploaderUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
