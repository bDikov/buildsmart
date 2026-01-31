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

    Task UpdateJobScopeAsync(Guid jobPostId, string newDetailsJson, string newDescription);
    
    Task<Bid> SubmitBidAsync(Guid tradesmanProfileId, Guid jobPostId, Amount amount, string? comment);
    
    Task<Booking> AcceptBidAsync(Guid bidId);
}
