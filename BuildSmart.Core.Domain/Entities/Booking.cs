using BuildSmart.Core.Domain.Common;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Core.Domain.ValueObjects;

// Note: BuildSmart.Core.Domain.Entities is not needed here
// because the namespace is already BuildSmart.Core.Domain.Entities

namespace BuildSmart.Core.Domain.Entities;

public class Booking : BaseEntity
{
	public DateTime RequestedDate { get; set; }
	public DateTime? ScheduledDate { get; set; }
	public string? JobDescription { get; set; }
	    public BookingStatusTypes Status { get; private set; } = BookingStatusTypes.Pending;
	
	    // --- Financials ---
	    public Amount AgreedBidAmount { get; set; } // The base cost of the work
	    public Amount PlatformFeeHomeowner { get; set; } // The 10% the homeowner pays
	    public Amount PlatformFeeTradesman { get; set; } // The 10% deducted from the tradesman
	    public Amount TotalEscrowAmount { get; set; } // Total amount held (Agreed + HomeownerFee)
	
	    public bool IsFunded { get; private set; } = false;
	
	    // --- Relationships ---
	    public Guid HomeownerId { get; set; }
	    public User Homeowner { get; set; } = null!;
	    
	    public Guid TradesmanProfileId { get; set; }
	    public TradesmanProfile TradesmanProfile { get; set; } = null!;
	
	    public Review? Review { get; set; }
	
	    public ICollection<ChangeOrder> ChangeOrders { get; set; } = new List<ChangeOrder>();
	
	    // --- Domain Logic Methods (DDD approach) ---
	public void ConfirmBooking(DateTime scheduledDate)
	{
		if (Status == BookingStatusTypes.Pending)
		{
			Status = BookingStatusTypes.Confirmed;
			ScheduledDate = scheduledDate;
			UpdatedAt = DateTime.UtcNow;
		}
	}

	public void SetCost(Amount cost)
	{
		if (Status != BookingStatusTypes.Pending)
		{
			throw new InvalidOperationException("Cost can only be set for pending bookings.");
		}
		AgreedBidAmount = cost;
		UpdatedAt = DateTime.UtcNow;
	}

	public void CancelBooking()
	{
		if (Status == BookingStatusTypes.Pending || Status == BookingStatusTypes.Confirmed)
		{
			Status = BookingStatusTypes.Cancelled;
			UpdatedAt = DateTime.UtcNow;
		}
	}

	public void CompleteBooking()
	{
		if (Status == BookingStatusTypes.Confirmed)
		{
			Status = BookingStatusTypes.Completed;
			UpdatedAt = DateTime.UtcNow;
		}
	}
}