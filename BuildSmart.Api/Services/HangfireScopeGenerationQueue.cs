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

    public ValueTask<string> QueueBackgroundWorkItemAsync(Guid jobPostId, CancellationToken cancellationToken)
    {
        var jobId = _backgroundJobClient.Enqueue<Workers.ScopeGenerationWorker>(worker => worker.ProcessJobAsync(jobPostId, CancellationToken.None));
        return ValueTask.FromResult(jobId);
    }

    public ValueTask<string> QueuePricingUpdateAsync(Guid jobPostId, CancellationToken cancellationToken)
    {
        var jobId = _backgroundJobClient.Enqueue<Workers.ScopeGenerationWorker>(worker => worker.ProcessPricingAsync(jobPostId, CancellationToken.None));
        return ValueTask.FromResult(jobId);
    }

    public ValueTask CancelJobAsync(string jobId)
    {
        _backgroundJobClient.Delete(jobId);
        return ValueTask.CompletedTask;
    }

    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Hangfire handles dequeuing automatically.");
    }
}