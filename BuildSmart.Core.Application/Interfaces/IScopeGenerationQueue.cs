using System.Threading.Channels;

namespace BuildSmart.Core.Application.Interfaces;

public interface IScopeGenerationQueue
{
    ValueTask<string> QueueBackgroundWorkItemAsync(Guid jobPostId, CancellationToken cancellationToken);
    ValueTask<string> QueuePricingUpdateAsync(Guid jobPostId, CancellationToken cancellationToken);
    ValueTask CancelJobAsync(string jobId);
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}
