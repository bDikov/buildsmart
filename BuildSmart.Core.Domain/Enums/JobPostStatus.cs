namespace BuildSmart.Core.Domain.Enums;

public enum JobPostStatus
{
    Draft = 0,
    UnderReview = 1, // Added
    Open = 2,        // Shifted from 1, but explicit now
    BiddingClosed = 3,
    Contracted = 4,
    Cancelled = 5,
    Expired = 6
}
