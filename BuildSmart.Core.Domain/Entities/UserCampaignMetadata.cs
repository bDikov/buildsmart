using BuildSmart.Core.Domain.Common;
using System;

namespace BuildSmart.Core.Domain.Entities;

public class UserCampaignMetadata : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public string UtmSource { get; set; } = null!;
    public string UtmMedium { get; set; } = null!;
    public string UtmCampaign { get; set; } = null!;
    public string? UtmContent { get; set; }
    public string? UtmTerm { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
