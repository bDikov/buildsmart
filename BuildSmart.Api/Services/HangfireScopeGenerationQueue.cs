using BuildSmart.Core.Application.Interfaces;
using Hangfire;

namespace BuildSmart.Api.Services;

public class HangfireScopeGenerationQueue : IScopeGenerationQueue
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireScopeGenerationQueue(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public ValueTask QueueBackgroundWorkItemAsync(Guid jobPostId, CancellationToken cancellationToken)
    {
        _backgroundJobClient.Enqueue<Workers.ScopeGenerationWorker>(worker => worker.ProcessJobAsync(jobPostId));
        return ValueTask.CompletedTask;
    }

    public ValueTask QueuePricingUpdateAsync(Guid jobPostId, CancellationToken cancellationToken)
    {
        _backgroundJobClient.Enqueue<Workers.ScopeGenerationWorker>(worker => worker.ProcessPricingAsync(jobPostId));
        return ValueTask.CompletedTask;
    }

    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Hangfire handles dequeuing automatically.");
    }
}