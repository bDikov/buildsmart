namespace BuildSmart.Core.Domain.Enums;

public enum JobPostStatus
{
    Draft = 0,
    GeneratingScope = 1,
    WaitingForUserReview = 2,
    WaitingForAdminReview = 3,
    UnderReview = 4, // Deprecated, use WaitingForAdminReview
    Open = 5,
    BiddingClosed = 6,
    Contracted = 7,
    Cancelled = 8,
    Expired = 9,
    Rejected = 10
}
