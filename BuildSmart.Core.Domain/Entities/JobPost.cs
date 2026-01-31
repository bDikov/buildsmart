using BuildSmart.Core.Domain.Common;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Core.Domain.ValueObjects;

namespace BuildSmart.Core.Domain.Entities;

public class JobPost : BaseEntity
{
    // --- Creator Info ---
    public Guid HomeownerProfileId { get; set; }
    public HomeownerProfile HomeownerProfile { get; set; } = null!;
    
    // --- Project Grouping ---
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    // --- Classification ---
    public Guid ServiceCategoryId { get; set; }
    public ServiceCategory ServiceCategory { get; set; } = null!;

    // --- Content ---
    public string Title { get; set; } = null!;
    
    /// <summary>
    /// Stores the answers to the Smart Blueprint template.
    /// Format: JSON.
    /// </summary>
    public string JobDetails { get; set; } = "{}";

    /// <summary>
    /// A human-readable summary derived from the JobDetails or user input.
    /// </summary>
    public string Description { get; set; } = null!;

    public string Location { get; set; } = null!;
    
    // Future: Use a separate entity for media if we need metadata, 
    // but a string array or list of URLs is fine for now.
    public List<string> ImageUrls { get; set; } = new();

    // --- Auction Mechanics ---
    public JobPostStatus Status { get; private set; } = JobPostStatus.Draft;
    
    public Amount? EstimatedBudget { get; set; }

    /// <summary>
    /// Tracks how many times the scope (JobDetails) has been modified.
    /// Bids must reference this version to be valid.
    /// </summary>
    public int AmendmentCount { get; private set; } = 0;

    // --- Navigation ---
    public ICollection<JobPostQuestion> Questions { get; set; } = new List<JobPostQuestion>();
    
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();

    // --- Domain Methods ---
    
    public void Publish()
    {
        if (Status == JobPostStatus.Draft || Status == JobPostStatus.UnderReview)
        {
            Status = JobPostStatus.Open;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SubmitForReview()
    {
        if (Status == JobPostStatus.Draft)
        {
            Status = JobPostStatus.UnderReview;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void CloseBidding()
    {
        if (Status == JobPostStatus.Open)
        {
            Status = JobPostStatus.BiddingClosed;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void UpdateScope(string newDetails, string newDescription)
    {
        JobDetails = newDetails;
        Description = newDescription;
        AmendmentCount++; // Increment version
        UpdatedAt = DateTime.UtcNow;
        
        // Logic to notify bidders would happen in a Domain Event or Service
    }
}
