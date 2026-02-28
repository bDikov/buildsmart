using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IJobsNotificationService
{
    /// <summary>
    /// Notifies relevant tradesmen that a new job has been published and is open for bidding.
    /// </summary>
    Task NotifyTradesmenOfNewJobAsync(JobPost jobPost);
}
