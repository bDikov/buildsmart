namespace BuildSmart.Api.DTOs;

public class SubmitBidInput
{
    public Guid TradesmanProfileId { get; set; }
    public Guid JobPostId { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Comment { get; set; }
    public DateTime? EarliestStartDate { get; set; }
    public DateTime? LatestStartDate { get; set; }
    public int? EstimatedDurationDays { get; set; }
    public List<BidItemInput> BidItems { get; set; } = new();
}

public class BidItemInput
{
    public Guid JobTaskId { get; set; }
    public decimal PriceSubtotal { get; set; }
    public string? Comment { get; set; }
}