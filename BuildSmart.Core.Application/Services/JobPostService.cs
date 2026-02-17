using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Core.Application.Services;

public class JobPostService : IJobPostService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScopeGenerationQueue _scopeGenerationQueue;

    public JobPostService(IUnitOfWork unitOfWork, IScopeGenerationQueue scopeGenerationQueue)
    {
        _unitOfWork = unitOfWork;
        _scopeGenerationQueue = scopeGenerationQueue;
    }

    public async Task SubmitJobForScopeGenerationAsync(Guid jobPostId)
    {
        var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId)
            ?? throw new ArgumentException("Job post not found");

        if (jobPost.Status != JobPostStatus.Draft && jobPost.Status != JobPostStatus.Rejected)
        {
            throw new InvalidOperationException("Only Draft or Rejected jobs can be submitted for scope generation.");
        }

        jobPost.SubmitForScopeGeneration();
        
        // Reset feedback when submitting a fix
        jobPost.AdminFeedback = null;

        _unitOfWork.JobPosts.Update(jobPost);
        await _unitOfWork.SaveChangesAsync();

        // Queue for background processing
        await _scopeGenerationQueue.QueueBackgroundWorkItemAsync(jobPost.Id, CancellationToken.None);
    }

    public async Task ApproveJobScopeAsync(Guid jobPostId, string finalScope)
    {
        var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId)
            ?? throw new ArgumentException("Job post not found");

        if (jobPost.Status == JobPostStatus.WaitingForAdminReview)
        {
            // Idempotency: If already waiting for admin, allow updating the text without throwing
            jobPost.UserEditedScope = finalScope;
            jobPost.Description = finalScope;
            jobPost.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            jobPost.ApproveScope(finalScope);
        }

        _unitOfWork.JobPosts.Update(jobPost);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task AdminReviewJobScopeAsync(Guid jobPostId, bool approved, string? feedback)
    {
        var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId)
            ?? throw new ArgumentException("Job post not found");

        if (approved)
        {
            if (jobPost.Status == JobPostStatus.Open)
            {
                // Idempotent: Already approved
                jobPost.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                jobPost.AdminApproveScope();
                jobPost.Publish();
            }
        }
        else
        {
            if (jobPost.Status == JobPostStatus.Rejected)
            {
                // Idempotent: Already rejected, just update feedback
                jobPost.AdminFeedback = feedback ?? "Rejected without feedback.";
                jobPost.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                jobPost.AdminRejectScope(feedback ?? "Rejected without feedback.");
            }
        }

        _unitOfWork.JobPosts.Update(jobPost);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<Project> CreateProjectAsync(Guid homeownerId, string title, string description)
    {
        var project = new Project
        {
            HomeownerId = homeownerId,
            Title = title,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Projects.AddAsync(project);
        await _unitOfWork.SaveChangesAsync();

        return project;
    }

    public async Task<JobPost> AddJobToProjectAsync(
        Guid projectId, 
        Guid categoryId, 
        string title, 
        string jobDetailsJson, 
        string location,
        Amount? estimatedBudget,
        List<string> imageUrls)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId)
            ?? throw new ArgumentException("Project not found");

        var category = await _unitOfWork.ServiceCategories.GetByIdAsync(categoryId)
            ?? throw new ArgumentException("Category not found");

        // Note: We need to handle the HomeownerProfileId correctly.
        // Usually, a User has a HomeownerProfile.
        var homeowner = await _unitOfWork.Users.GetByIdAsync(project.HomeownerId);
        if (homeowner?.HomeownerProfile == null)
        {
             throw new InvalidOperationException("User does not have a Homeowner profile. Please complete your profile first.");
        }

        var jobPost = new JobPost
        {
            ProjectId = projectId,
            ServiceCategoryId = categoryId,
            HomeownerProfileId = homeowner.HomeownerProfile.Id, // Set correct Profile ID
            Title = title,
            JobDetails = jobDetailsJson,
            Description = $"Job for {category.Name}: {title}", // Placeholder, ideally generated from Wizard
            Location = location,
            EstimatedBudget = estimatedBudget,
            ImageUrls = imageUrls,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Auto-trigger scope generation
        jobPost.SubmitForScopeGeneration();

        await _unitOfWork.JobPosts.AddAsync(jobPost);
        await _unitOfWork.SaveChangesAsync();

        // Fire and forget background task
        await _scopeGenerationQueue.QueueBackgroundWorkItemAsync(jobPost.Id, CancellationToken.None);

        return jobPost;
    }

    public async Task UpdateJobScopeAsync(Guid jobPostId, string newDetailsJson, string newDescription)
    {
        var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId)
            ?? throw new ArgumentException("Job post not found");

        jobPost.UpdateScope(newDetailsJson, newDescription);
        
        _unitOfWork.JobPosts.Update(jobPost);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task SaveDraftAsync(Guid jobPostId, string jobDetailsJson, string? description, string? location, Amount? estimatedBudget)
    {
        var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId)
            ?? throw new ArgumentException("Job post not found");

        if (jobPost.Status != JobPostStatus.Draft)
        {
             // We allow saving draft only if it is already draft? 
             // Or maybe we allow editing active jobs back to draft? 
             // For now, let's assume this is for the creation wizard.
             // If it's already published, use UpdateScope (Amendment).
        }

        jobPost.JobDetails = jobDetailsJson;
        if (!string.IsNullOrEmpty(description)) jobPost.Description = description;
        if (!string.IsNullOrEmpty(location)) jobPost.Location = location;
        if (estimatedBudget != null) jobPost.EstimatedBudget = estimatedBudget;
        
        // Ensure status is Draft (in case it was something else, though usually it stays Draft)
        // jobPost.Status = JobPostStatus.Draft; // Setter is private, need method or reflection?
        // Ideally we have a method RevertToDraft() or similar if needed, 
        // but typically we just update the fields.
        
        jobPost.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.JobPosts.Update(jobPost);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task SubmitJobPostAsync(Guid jobPostId)
    {
        var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId)
            ?? throw new ArgumentException("Job post not found");
        
        // 1. Get Categories (Specific + Global)
        var specificCategory = await _unitOfWork.ServiceCategories.GetByIdAsync(jobPost.ServiceCategoryId);
        var globalCategories = await _unitOfWork.ServiceCategories.GetQueryable().Where(c => c.IsGlobal && c.Status == CategoryStatus.Active).ToListAsync();

        var allCategories = new List<ServiceCategory>();
        if (specificCategory != null) allCategories.Add(specificCategory);
        allCategories.AddRange(globalCategories);

        // 2. Validate Answers
        ValidateMandatoryQuestions(allCategories, jobPost.JobDetails);

        // 3. Transition Status
        jobPost.SubmitForReview();
        
        // 4. Also update Project status if needed
        // If the project is in Draft, submitting a job should move the project to UnderReview.
        if (jobPost.ProjectId != Guid.Empty)
        {
             var project = await _unitOfWork.Projects.GetByIdAsync(jobPost.ProjectId);
             if (project != null && project.Status == ProjectStatus.Draft)
             {
                 project.SubmitForReview();
                 _unitOfWork.Projects.Update(project);
             }
        }

        _unitOfWork.JobPosts.Update(jobPost);
        await _unitOfWork.SaveChangesAsync();
    }

    private void ValidateMandatoryQuestions(List<ServiceCategory> categories, string jobDetailsJson)
    {
        // Parse Answers
        using var doc = System.Text.Json.JsonDocument.Parse(jobDetailsJson);
        var root = doc.RootElement;

        foreach (var cat in categories)
        {
            if (string.IsNullOrEmpty(cat.TemplateStructure) || cat.TemplateStructure == "{}") continue;

            try 
            {
                using var templateDoc = System.Text.Json.JsonDocument.Parse(cat.TemplateStructure);
                if (templateDoc.RootElement.TryGetProperty("questions", out var questionsElement))
                {
                    foreach (var q in questionsElement.EnumerateArray())
                    {
                        // Check if required
                        bool isRequired = false;
                        if (q.TryGetProperty("isRequired", out var reqProp))
                        {
                            isRequired = reqProp.GetBoolean();
                        }

                        if (isRequired)
                        {
                            var qId = q.GetProperty("id").GetString();
                            if (string.IsNullOrEmpty(qId)) continue;

                            // Check if answer exists and is not empty
                            if (!root.TryGetProperty(qId, out var answerProp) || 
                                (answerProp.ValueKind == System.Text.Json.JsonValueKind.String && string.IsNullOrWhiteSpace(answerProp.GetString())) ||
                                answerProp.ValueKind == System.Text.Json.JsonValueKind.Null)
                            {
                                var qText = q.TryGetProperty("text", out var textProp) ? textProp.GetString() : "Unknown Question";
                                throw new InvalidOperationException($"Missing mandatory answer for: {qText}");
                            }
                        }
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Ignore malformed templates for now, or log warning
            }
        }
    }

    public async Task<Bid> SubmitBidAsync(Guid tradesmanProfileId, Guid jobPostId, Amount amount, string? comment)
    {
        var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId)
            ?? throw new ArgumentException("Job post not found");

        var bid = new Bid
        {
            JobPostId = jobPostId,
            TradesmanProfileId = tradesmanProfileId,
            Amount = amount,
            Comment = comment,
            LinkedAmendmentVersion = jobPost.AmendmentCount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Bids.AddAsync(bid);
        await _unitOfWork.SaveChangesAsync();

        return bid;
    }

    public async Task<Booking> AcceptBidAsync(Guid bidId)
    {
        var bid = await _unitOfWork.Bids.GetByIdAsync(bidId)
            ?? throw new ArgumentException("Bid not found");

        if (bid.IsOutdated(bid.JobPost.AmendmentCount))
        {
            throw new InvalidOperationException("Cannot accept an outdated bid.");
        }

        bid.Accept();

        // Calculate Fees (10% each side)
        var feeRate = 0.10m;
        var feeAmountValue = bid.Amount.Total * feeRate;
        
        var feeHomeowner = Amount.Create(bid.Amount.Currency, feeAmountValue, 0);
        var feeTradesman = Amount.Create(bid.Amount.Currency, feeAmountValue, 0);
        var totalEscrow = Amount.Create(bid.Amount.Currency, bid.Amount.Total + feeAmountValue, 0);

        var booking = new Booking
        {
            HomeownerId = bid.JobPost.Project.HomeownerId,
            TradesmanProfileId = bid.TradesmanProfileId,
            AgreedBidAmount = bid.Amount,
            PlatformFeeHomeowner = feeHomeowner,
            PlatformFeeTradesman = feeTradesman,
            TotalEscrowAmount = totalEscrow,
            JobDescription = bid.JobPost.Description,
            RequestedDate = DateTime.UtcNow, // Or from project?
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Bookings.AddAsync(booking);
        
        // Close bidding on job post
        bid.JobPost.CloseBidding();
        
        await _unitOfWork.SaveChangesAsync();

        return booking;
    }

    public async Task<JobPostFeedback> AddFeedbackAsync(Guid jobPostId, Guid authorId, string text)
    {
        var jobPost = await _unitOfWork.JobPosts.GetByIdAsync(jobPostId)
            ?? throw new ArgumentException("Job post not found");

        var feedback = new JobPostFeedback
        {
            JobPostId = jobPostId,
            AuthorId = authorId,
            Text = text,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.JobPostFeedbacks.AddAsync(feedback);
        
        // If it's the homeowner responding to a rejected job, 
        // we might want to automatically signal that it's ready for review again
        // but we'll leave that to the specific workflow actions for now.

        await _unitOfWork.SaveChangesAsync();
        return feedback;
    }

    public async Task ResolveFeedbackAsync(Guid feedbackId)
    {
        var feedback = await _unitOfWork.JobPostFeedbacks.GetByIdAsync(feedbackId)
            ?? throw new ArgumentException("Feedback not found");

        feedback.IsResolved = true;
        feedback.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.JobPostFeedbacks.Update(feedback);
        await _unitOfWork.SaveChangesAsync();
    }
}
