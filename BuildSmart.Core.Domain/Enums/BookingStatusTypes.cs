namespace BuildSmart.Core.Domain.Enums;

public enum BookingStatusTypes
{
    PendingDeposit, // Homeowner accepted bid, waiting for deposit to escrow
    InProgress,     // Funds are in escrow, work can begin
    Completed,      // All milestones are paid out
    Disputed,       // A milestone or the overall booking is in dispute
    Cancelled       // Booking was cancelled before completion
}