using BuildSmart.Core.Domain.Common;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Core.Domain.ValueObjects;

namespace BuildSmart.Core.Domain.Entities;

public class Booking : BaseEntity
{
    public BookingStatusTypes Status { get; private set; } = BookingStatusTypes.PendingDeposit;

    // --- Financials ---
    public Amount AgreedBidAmount { get; set; } = null!; // The base cost of the work
    public Amount PlatformFeeHomeowner { get; set; } = null!; // Escrow/Platform fee charged to homeowner
    public Amount PlatformFeeTradesman { get; set; } = null!; // Fee deducted from tradesman payout
    public Amount TotalEscrowAmount { get; set; } = null!; // Total amount held (Agreed + HomeownerFee)

    public bool IsFunded { get; private set; } = false;

    // --- Relationships ---
    public Guid HomeownerId { get; set; }
    public User Homeowner { get; set; } = null!;

    public Guid TradesmanProfileId { get; set; }
    public TradesmanProfile TradesmanProfile { get; set; } = null!;

    public Guid JobPostId { get; set; }
    public JobPost JobPost { get; set; } = null!;

    public Guid BidId { get; set; }
    public Bid Bid { get; set; } = null!;

    public Review? Review { get; set; }

    public ICollection<MilestonePayment> MilestonePayments { get; set; } = new List<MilestonePayment>();
    public ICollection<ChangeOrder> ChangeOrders { get; set; } = new List<ChangeOrder>();

    // --- Domain Logic Methods ---
    public void ConfirmDeposit()
    {
        if (Status == BookingStatusTypes.PendingDeposit)
        {
            Status = BookingStatusTypes.InProgress;
            IsFunded = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void CancelBooking()
    {
        if (Status == BookingStatusTypes.PendingDeposit || Status == BookingStatusTypes.InProgress)
        {
            Status = BookingStatusTypes.Cancelled;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void CompleteBooking()
    {
        if (Status == BookingStatusTypes.InProgress)
        {
            Status = BookingStatusTypes.Completed;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void MarkDisputed()
    {
        Status = BookingStatusTypes.Disputed;
        UpdatedAt = DateTime.UtcNow;
    }
}