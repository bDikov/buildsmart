using BuildSmart.Core.Domain.Common;
using BuildSmart.Core.Domain.Enums;

namespace BuildSmart.Core.Domain.Entities;

/// <summary>
/// Represents a high-quality video (Reel) or picture media item in the tradesman's portfolio feed.
/// </summary>
public class TradesmanMedia : BaseEntity
{
    public Guid TradesmanId { get; set; }
    public TradesmanProfile TradesmanProfile { get; set; } = null!;

    public string VideoUrl { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    
    public MediaType Type { get; set; } = MediaType.Video;

    public Guid? ServiceCategoryId { get; set; }
    public ServiceCategory? ServiceCategory { get; set; }

    public bool IsActive { get; set; } = true;
}