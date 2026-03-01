using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class Certification : BaseEntity
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string DocumentUrl { get; set; } = null!;
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    // --- Relationships ---

    public Guid TradesmanProfileId { get; set; }
    public TradesmanProfile TradesmanProfile { get; set; } = null!;
}
