using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

/// <summary>
/// Represents raw verification photos or videos linked to a specific completed project/milestone.
/// </summary>
public class ProjectMilestoneMedia : BaseEntity
{
    public Guid TradesmanProfileId { get; set; }
    public TradesmanProfile TradesmanProfile { get; set; } = null!;

    public Guid JobId { get; set; }
    public JobPost Job { get; set; } = null!;

    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// e.g. "Video" or "Image"
    /// </summary>
    public string Type { get; set; } = string.Empty;
}