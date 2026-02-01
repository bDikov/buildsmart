using BuildSmart.Core.Domain.Common;
using BuildSmart.Core.Domain.Enums;

namespace BuildSmart.Core.Domain.Entities;

public class Project : BaseEntity
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    
    // The Homeowner who owns this project
    public Guid HomeownerId { get; set; }
    public User Homeowner { get; set; } = null!;

    // A project can consist of multiple specific jobs (e.g., "Plumbing", "Electrical")
    public ICollection<JobPost> JobPosts { get; set; } = new List<JobPost>();

    // Overall status of the project
    public ProjectStatus Status { get; private set; } = ProjectStatus.Draft;

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
    
    public void Archive()
    {
        Status = ProjectStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }
}
