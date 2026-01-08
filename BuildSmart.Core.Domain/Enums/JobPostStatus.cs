namespace BuildSmart.Core.Domain.Enums;

public enum JobPostStatus
{
    Draft,
    Open,            // Live, accepting bids
    BiddingClosed,   // Deadline passed or manually paused
    Contracted,      // Winner selected, Project created
    Cancelled,
    Expired          // System auto-closed after X days
}
