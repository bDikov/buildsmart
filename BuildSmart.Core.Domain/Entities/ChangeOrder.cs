using BuildSmart.Core.Domain.Common;
using BuildSmart.Core.Domain.ValueObjects;

namespace BuildSmart.Core.Domain.Entities;

public class ChangeOrder : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    /// <summary>
    /// The new total amount proposed for the job.
    /// </summary>
    public Amount NewTotalAmount { get; set; }

    /// <summary>
    /// The difference from the previous amount.
    /// </summary>
    public Amount DifferenceAmount { get; set; }

    public string Reason { get; set; } = null!;

    public ChangeOrderStatus Status { get; private set; } = ChangeOrderStatus.Pending;

    public void Approve()
    {
        if (Status == ChangeOrderStatus.Pending)
        {
            Status = ChangeOrderStatus.Approved;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Reject()
    {
        if (Status == ChangeOrderStatus.Pending)
        {
            Status = ChangeOrderStatus.Rejected;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
