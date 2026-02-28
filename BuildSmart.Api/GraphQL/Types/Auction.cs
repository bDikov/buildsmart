using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Api.GraphQL.Types;

/// <summary>
/// A specialized GraphQL wrapper for a JobPost that represents an active bidding event.
/// Aggregates related data like current bids and Q&A from the perspective of a tradesman.
/// </summary>
public class Auction
{
    public JobPost Job { get; set; } = null!;
    
    // We can add logic to filter what the tradesman sees (e.g. only their own bids vs all bids)
    public IEnumerable<Bid> Bids { get; set; } = Enumerable.Empty<Bid>();
    
    public IEnumerable<JobPostQuestion> Questions { get; set; } = Enumerable.Empty<JobPostQuestion>();

    // Future: Tours, etc.
}
