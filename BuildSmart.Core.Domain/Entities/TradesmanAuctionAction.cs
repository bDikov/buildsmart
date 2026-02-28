using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public enum AuctionActionType
{
    Passed,
    Interested,
    Saved
}

public class TradesmanAuctionAction : BaseEntity
{
    public Guid TradesmanProfileId { get; set; }
    public TradesmanProfile TradesmanProfile { get; set; } = null!;

    public Guid JobPostId { get; set; }
    public JobPost JobPost { get; set; } = null!;

    public AuctionActionType ActionType { get; set; }
}
