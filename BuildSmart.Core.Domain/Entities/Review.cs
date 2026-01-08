using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class Review : BaseEntity
{
	/// <summary>
	/// The rating given by the user, e.g., from 1 to 5 stars.
	/// </summary>
	public int Rating { get; set; }

	/// <summary>
	/// The written comment from the user.
	/// </summary>
	public string? Comment { get; set; }

	// --- Relationships ---

	// One-to-One relationship with Booking. A review must be linked to a completed booking.
	public Guid BookingId { get; set; }

	public Booking Booking { get; set; } = null!;

	// Foreign key to the User (Homeowner) who wrote the review.
	public Guid HomeownerId { get; set; }

	public User Homeowner { get; set; } = null!;

	// Foreign key to the TradesmanProfile that is being reviewed.
	public Guid TradesmanProfileId { get; set; }

	public TradesmanProfile TradesmanProfile { get; set; } = null!;
}