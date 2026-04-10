using BuildSmart.Core.Domain.Common;
using BuildSmart.Core.Domain.ValueObjects;

namespace BuildSmart.Core.Domain.Entities;

public class BidItem : BaseEntity
{
    public Guid BidId { get; set; }
    public Bid Bid { get; set; } = null!;

    public Guid JobTaskId { get; set; }
    public JobTask JobTask { get; set; } = null!;

    public Amount Price { get; set; } = null!;
    public string? Comment { get; set; }
}