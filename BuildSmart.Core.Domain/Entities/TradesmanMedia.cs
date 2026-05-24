using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

/// <summary>
/// Represents a high-quality video (Reel) or media item in the tradesman's portfolio feed.
/// </summary>
public class TradesmanMedia : BaseEntity
{
    public Guid TradesmanId { get; set; }
    public TradesmanProfile TradesmanProfile { get; set; } = null!;

    public string VideoUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public bool IsActive { get; set; } = true;
}