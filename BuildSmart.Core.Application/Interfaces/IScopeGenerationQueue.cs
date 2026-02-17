using System.Threading.Channels;

namespace BuildSmart.Core.Application.Interfaces;

public interface IScopeGenerationQueue
{
    ValueTask QueueBackgroundWorkItemAsync(Guid jobPostId, CancellationToken cancellationToken);
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}
