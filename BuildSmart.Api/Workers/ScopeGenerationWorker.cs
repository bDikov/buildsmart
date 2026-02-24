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

    public ScopeGenerationWorker(
        IScopeGenerationQueue taskQueue,
        IServiceProvider serviceProvider,
        ILogger<ScopeGenerationWorker> logger)
    {
        _taskQueue = taskQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;
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
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            // 1. Generate the Scope using AI
            var generatedScope = await aiService.GenerateJobScopeAsync(jobPost);

            // 2. Update the Job Post
            jobPost.SetGeneratedScope(generatedScope);

            // 3. Save Changes
            unitOfWork.JobPosts.Update(jobPost);
            await unitOfWork.SaveChangesAsync();

            // 4. Send Real-time Notification
            await notificationService.SendNotificationAsync(
                jobPost.Project.HomeownerId,
                "Scope Ready",
                $"The AI scope for '{jobPost.Title}' is ready for your review.",
                jobPost.Id,
                "JobPost"
            );

            _logger.LogInformation("Scope generated successfully for Job {JobId}.", jobPostId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate scope for Job {JobId}.", jobPostId);
            
            try 
            {
                jobPost.MarkGenerationFailed(ex.Message);
                unitOfWork.JobPosts.Update(jobPost);
                await unitOfWork.SaveChangesAsync();
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Failed to update JobPost status after generation failure.");
            }
        }
    }
}
