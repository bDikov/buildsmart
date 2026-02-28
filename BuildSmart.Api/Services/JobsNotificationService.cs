using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Api.Services;

public class JobsNotificationService : IJobsNotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public JobsNotificationService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task NotifyTradesmenOfNewJobAsync(JobPost jobPost)
    {
        // 1. Identify tradesmen with skills matching the job's category
        // In a real scenario, we might also filter by location (jobPost.Location)
        var matchingTradesmen = await _unitOfWork.TradesmanProfiles.GetQueryable()
            .Where(tp => tp.Skills.Any(s => s.ServiceCategoryId == jobPost.ServiceCategoryId))
            .ToListAsync();

        // 2. Send notifications to all matching tradesmen
        foreach (var tradesman in matchingTradesmen)
        {
            await _notificationService.SendNotificationAsync(
                tradesman.UserId,
                "New Job Opportunity",
                $"A new job matching your skills is available: '{jobPost.Title}'",
                jobPost.Id,
                "JobPost",
                new { route = "AuctionHub", jobId = jobPost.Id }
            );
        }
    }
}
