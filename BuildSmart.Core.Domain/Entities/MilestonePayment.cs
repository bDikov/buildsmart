using BuildSmart.Core.Domain.Common;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Core.Domain.ValueObjects;

namespace BuildSmart.Core.Domain.Entities;

public class MilestonePayment : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public Guid JobTaskId { get; set; }
    public JobTask JobTask { get; set; } = null!;

    public Amount AmountAllocated { get; set; } = null!;
    
    public MilestoneStatus Status { get; private set; } = MilestoneStatus.Pending;
    
    public string? StripeTransferId { get; set; }

    public void Approve()
    {
        if (Status == MilestoneStatus.Pending)
        {
            Status = MilestoneStatus.Approved;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void MarkPaid(string transferId)
    {
        if (Status == MilestoneStatus.Approved)
        {
            Status = MilestoneStatus.Paid;
            StripeTransferId = transferId;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void MarkDisputed()
    {
        Status = MilestoneStatus.Disputed;
        UpdatedAt = DateTime.UtcNow;
    }
}
