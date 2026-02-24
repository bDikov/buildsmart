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

	/// <summary>
	/// The AI-generated scope of work based on the Q&A.
	/// </summary>
	public string? GeneratedScope { get; set; }

	/// <summary>
	/// The user's edited version of the scope (if modified).
	/// </summary>
	public string? UserEditedScope { get; set; }

	/// <summary>
	/// Feedback from the admin if the scope is rejected.
	/// </summary>
	public string? AdminFeedback { get; set; }

	/// <summary>
	/// Stores project-specific clarification questions added by admins.
	/// Uses the same JSON structure as ServiceCategory.TemplateStructure.
	/// </summary>
	public string? AdditionalQuestionsJson { get; set; }

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

	public ICollection<JobPostFeedback> Feedbacks { get; set; } = new List<JobPostFeedback>();

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
			Status = JobPostStatus.WaitingForAdminReview;
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
	
	    public void SubmitForScopeGeneration()
	    {
	        if (Status == JobPostStatus.Draft || Status == JobPostStatus.Rejected || Status == JobPostStatus.WaitingForUserReview)
	        {
	            Status = JobPostStatus.GeneratingScope;
	            UpdatedAt = DateTime.UtcNow;
	        }
	        else
	        {
	            throw new InvalidOperationException($"Cannot submit for scope generation from status {Status}");
	        }
	    }
	
	    public void SetGeneratedScope(string scope)
    {
        if (Status != JobPostStatus.GeneratingScope)
        {
            // If it's not in GeneratingScope, maybe ignore? Or throw?
            // For robustness, if it's already past this stage, we shouldn't overwrite.
            throw new InvalidOperationException($"Cannot set generated scope when status is {Status}");
        }

        GeneratedScope = scope;
        Status = JobPostStatus.WaitingForUserReview;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkGenerationFailed(string error)
    {
        if (Status == JobPostStatus.GeneratingScope)
        {
            Status = JobPostStatus.Rejected;
            AdminFeedback = $"AI Generation Error: {error}";
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void ApproveScope(string finalScope)
	    {
	        if (Status != JobPostStatus.WaitingForUserReview)
	        {
	            throw new InvalidOperationException($"Job is not waiting for user review. Current Status: {Status}");
	        }
	        
	        UserEditedScope = finalScope;
	        Description = finalScope;
	        Status = JobPostStatus.WaitingForAdminReview;
	        UpdatedAt = DateTime.UtcNow;
	    }
	
	    public void AdminApproveScope()
	    {
	        if (Status != JobPostStatus.WaitingForAdminReview)
	        {
	            throw new InvalidOperationException($"Job is not waiting for admin review. Current Status: {Status}");
	        }
	        
	        Status = JobPostStatus.Open;
	        UpdatedAt = DateTime.UtcNow;
	    }
	
	    	    public void AdminRejectScope(string feedback)
	    	    {
	    	        if (Status != JobPostStatus.WaitingForAdminReview)
	    	        {
	    	            throw new InvalidOperationException($"Job is not waiting for admin review. Current Status: {Status}");
	    	        }
	    	        
	    	        Status = JobPostStatus.Rejected;
	    	        AdminFeedback = feedback;
	    	        UpdatedAt = DateTime.UtcNow;
	    	    }
	    
	    	    	    public void ResubmitAfterClarification()
	    	    	    {
	    	    	        if (Status == JobPostStatus.Rejected)
	    	    	        {
	    	    	            Status = JobPostStatus.WaitingForAdminReview;
	    	    	            UpdatedAt = DateTime.UtcNow;
	    	    	        }
	    	    	    }
	    	    
	    	    	    public void RequestUserReview()
	    	    	    {
	    	    	        if (Status == JobPostStatus.WaitingForAdminReview || Status == JobPostStatus.Rejected)
	    	    	        {
	    	    	            Status = JobPostStatus.WaitingForUserReview;
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