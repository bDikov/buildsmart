using System.Threading.Channels;
using BuildSmart.Core.Application.Interfaces;

namespace BuildSmart.Infrastructure.Services;

public class ScopeGenerationQueue : IScopeGenerationQueue
{
    private readonly Channel<Guid> _queue;

    public ScopeGenerationQueue()
    {
        // Thread-safe channel for processing job IDs
        var options = new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<Guid>(options);
    }

    public async ValueTask<string> QueueBackgroundWorkItemAsync(Guid jobPostId, CancellationToken cancellationToken)
    {
        await _queue.Writer.WriteAsync(jobPostId, cancellationToken);
        return Guid.NewGuid().ToString();
    }

    public async ValueTask<string> QueuePricingUpdateAsync(Guid jobPostId, CancellationToken cancellationToken)
    {
        // This is a legacy queue. We write to the same channel, but the BackgroundService 
        // won't know to execute pricing instead of generation. 
        // Using Hangfire handles this properly.
        await _queue.Writer.WriteAsync(jobPostId, cancellationToken);
        return Guid.NewGuid().ToString();
    }

    public ValueTask CancelJobAsync(string jobId)
    {
        // Not supported in legacy queue
        return ValueTask.CompletedTask;
    }

    public async ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
