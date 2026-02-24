using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.ValueObjects;

namespace BuildSmart.Core.Application.Interfaces;

public interface IJobPostService
{
    Task<Project> CreateProjectAsync(Guid homeownerId, string title, string description);
    
    Task<JobPost> AddJobToProjectAsync(
        Guid projectId, 
        Guid categoryId, 
        string title, 
        string jobDetailsJson,
        string location,
        Amount? estimatedBudget,
        List<string> imageUrls);
    
    Task SubmitJobForScopeGenerationAsync(Guid jobPostId);
    
    Task ApproveJobScopeAsync(Guid jobPostId, string finalScope);
    
    Task AdminReviewJobScopeAsync(Guid jobPostId, bool approved, string? feedback, Guid? reviewerId);

    Task UpdateJobScopeAsync(Guid jobPostId, string newDetailsJson, string newDescription);
            
            Task SaveDraftAsync(Guid jobPostId, string jobDetailsJson, string? description, string? location, Amount? estimatedBudget);
            
            Task SubmitJobPostAsync(Guid jobPostId);
        
            Task<Bid> SubmitBidAsync(Guid tradesmanProfileId, Guid jobPostId, Amount amount, string? comment);    
    Task<Booking> AcceptBidAsync(Guid bidId);

    Task<JobPostFeedback> AddFeedbackAsync(Guid jobPostId, Guid authorId, string text);
    Task ResolveFeedbackAsync(Guid feedbackId);

    Task<bool> AddAdminQuestionAsync(Guid jobPostId, string questionText, string type, bool isRequired, List<string>? options = null);
}
