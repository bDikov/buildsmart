using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Core.Domain.ValueObjects;

namespace BuildSmart.Core.Application.Services;

public class JobPostService : IJobPostService
{
    private readonly IUnitOfWork _unitOfWork;

    public JobPostService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

        var jobPost = new JobPost
        {
            ProjectId = projectId,
            ServiceCategoryId = categoryId,
            HomeownerProfileId = project.HomeownerId, // Assuming HomeownerId is the same as ProfileId for now, but let's check
            Title = title,
            JobDetails = jobDetailsJson,
            Description = $"Job for {category.Name}: {title}", // Placeholder, ideally generated from Wizard
            Location = location,
            EstimatedBudget = estimatedBudget,
            ImageUrls = imageUrls,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Note: We need to handle the HomeownerProfileId correctly.
        // Usually, a User has a HomeownerProfile.
        var homeowner = await _unitOfWork.Users.GetByIdAsync(project.HomeownerId);
        if (homeowner?.HomeownerProfile == null)
        {
             throw new InvalidOperationException("User does not have a Homeowner profile. Please complete your profile first.");
        }
        else
        {
            jobPost.HomeownerProfileId = homeowner.HomeownerProfile.Id;
        }

        await _unitOfWork.JobPosts.AddAsync(jobPost);
        await _unitOfWork.SaveChangesAsync();

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
}
