using BuildSmart.Core.Domain.Common;
using BuildSmart.Core.Domain.Enums;

namespace BuildSmart.Core.Domain.Entities;

public class Project : BaseEntity
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    
    public string LanguageCode { get; set; } = "bg";
    
    // The Homeowner who owns this project
    public Guid HomeownerId { get; set; }
    public User Homeowner { get; set; } = null!;

    /// <summary>
    /// Admin markup percentage added on top of Tradesman base prices (e.g. 20.0 for 20% markup)
    /// </summary>
    public decimal AdminMarkupPercentage { get; set; } = 20.0m;

    // A project can consist of multiple specific jobs (e.g., "Plumbing", "Electrical")
    public ICollection<JobPost> JobPosts { get; set; } = new List<JobPost>();

    /// <summary>
    /// High-level summary of the entire project, aggregated from all job posts.
    /// </summary>
    public string? GeneralSummary { get; set; }

    /// <summary>
    /// The generated Master Offer PDF file.
    /// </summary>
    public byte[]? MasterOfferPdf { get; set; }

    public bool HasOfferPdf => MasterOfferPdf != null && MasterOfferPdf.Length > 0;

    // Overall status of the project
    public ProjectStatus Status { get; private set; } = ProjectStatus.Draft;

    public int? LastVisitedStep { get; set; }

    public void SubmitForReview()
    {
        // Allow transition from Draft to UnderReview
        if (Status == ProjectStatus.Draft)
        {
            Status = ProjectStatus.UnderReview;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Publish()
    {
        Status = ProjectStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = ProjectStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetToDraft()
    {
        Status = ProjectStatus.Draft;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Archive()
    {
        Status = ProjectStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }
}
