using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildSmart.Api.Workers;

public class ScopeGenerationWorker : BackgroundService
{
    private readonly IScopeGenerationQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScopeGenerationWorker> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;

    public ScopeGenerationWorker(
        IScopeGenerationQueue taskQueue,
        IServiceProvider serviceProvider,
        ILogger<ScopeGenerationWorker> logger,
        IHubContext<NotificationHub> hubContext)
    {
        _taskQueue = taskQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scope Generation Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var jobPostId = await _taskQueue.DequeueAsync(stoppingToken);

                _logger.LogInformation("Processing Job Scope for Job ID: {JobId}", jobPostId);

                await ProcessJobAsync(jobPostId, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing Scope Generation task.");
            }
        }
    }

    private async Task ProcessJobAsync(Guid jobPostId, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();

        var jobPost = await unitOfWork.JobPosts.GetByIdAsync(jobPostId);
        if (jobPost == null)
        {
            _logger.LogWarning("Job Post {JobId} not found.", jobPostId);
            return;
        }

        try
        {
            // 1. Generate the Scope using AI
            var generatedScope = await aiService.GenerateJobScopeAsync(jobPost);

            // 2. Update the Job Post
            jobPost.SetGeneratedScope(generatedScope);

            // 3. Save Changes
            unitOfWork.JobPosts.Update(jobPost);
            
            // 4. Create Notification
            var notification = new Notification
            {
                UserId = jobPost.Project.HomeownerId,
                Title = "Scope Ready",
                Message = $"The AI scope for '{jobPost.Title}' is ready for your review.",
                RelatedEntityId = jobPost.Id,
                RelatedEntityType = "JobPost",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await unitOfWork.Notifications.AddAsync(notification);

            await unitOfWork.SaveChangesAsync();

            // 5. Send Real-time Notification
            await _hubContext.Clients.Group(notification.UserId.ToString())
                .SendAsync("ReceiveNotification", notification.Title, notification.Message);

            _logger.LogInformation("Scope generated successfully for Job {JobId}.", jobPostId);

            // Optional: Trigger notification to user here
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate scope for Job {JobId}.", jobPostId);
            // Consider setting status to 'Draft' or adding an error flag so user can retry
        }
    }
}
