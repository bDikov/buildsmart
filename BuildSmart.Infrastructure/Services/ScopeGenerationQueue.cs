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

    public async ValueTask QueueBackgroundWorkItemAsync(Guid jobPostId, CancellationToken cancellationToken)
    {
        await _queue.Writer.WriteAsync(jobPostId, cancellationToken);
    }

    public async ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
