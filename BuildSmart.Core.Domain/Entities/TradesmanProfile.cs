using BuildSmart.Core.Domain.Common;
using BuildSmart.Core.Domain.Entities.JoinEntities;

namespace BuildSmart.Core.Domain.Entities;

/// <summary>
/// Represents the specific profile for a tradesman,
/// extending the base User entity with role-specific data.
/// </summary>
public class TradesmanProfile : BaseEntity
{
	// --- Foreign Keys ---

	/// <summary>
	/// Foreign key to the main User account.
	/// This creates a one-to-one relationship.
	/// </summary>
	public Guid UserId { get; set; }

	public User User { get; set; } = null!;

	// --- Role-Specific Properties ---

	/// <summary>
	/// The average rating calculated from all verified reviews.
	/// This property has a private setter to protect it from external changes.
	/// </summary>
	public double AverageRating { get; private set; } = 0;

	/// <summary>
	/// Indicates if the tradesman's identity and qualifications
	/// have been verified by an admin.
	/// This property also has a private setter.
	/// </summary>
	public bool IsVerified { get; private set; } = false;

	// --- Navigation Properties ---

    /// <summary>
    /// The specific skills/trades this tradesman offers.
    /// </summary>
    public ICollection<TradesmanSkill> Skills { get; set; } = [];

	/// <summary>
	/// A collection of portfolio entries showcasing the tradesman's work.
	/// </summary>
	public ICollection<PortfolioEntry> PortfolioEntries { get; set; } = [];

	/// <summary>
	/// A collection of bookings associated with this tradesman.
	/// </summary>
	public ICollection<Booking> Bookings { get; set; } = [];

	/// <summary>
	/// A collection of reviews left for this tradesman.
	/// </summary>
	public ICollection<Review> Reviews { get; set; } = [];

	// --- Domain Logic Methods ---

	/// <summary>
	/// Business logic to mark this tradesman as verified.
	/// This is a domain operation, called by an application service.
	/// </summary>
	public void Verify()
	{
		IsVerified = true;
		// In a real scenario, you might also dispatch a Domain Event here.
	}

	/// <summary>
	/// Business logic to update the average rating.
	/// This should be called by a service when a new review is added.
	/// </summary>
	public void UpdateRating()
	{
		// This is a simple implementation. A real one would be a weighted average.
		if (Reviews.Any())
		{
			AverageRating = Reviews.Average(r => r.Rating);
		}
		else
		{
			AverageRating = 0;
		}
	}
}