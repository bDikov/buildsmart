using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

/// <summary>
/// Defines the contract for a repository handling Review data operations.
/// </summary>
public interface IReviewRepository
{
	/// <summary>
	/// Gets a review by its unique identifier.
	/// </summary>
	/// <param name="id">The review's unique identifier.</param>
	/// <returns>The Review object or null if not found.</returns>
	Task<Review?> GetByIdAsync(Guid id);

	/// <summary>
	/// Adds a new review to the repository.
	/// Per DDD, reviews are typically not updated or deleted.
	/// </summary>
	/// <param name="review">The review object to add.</param>
	Task AddAsync(Review review);

	/// <summary>
	/// Gets all reviews for a specific tradesman.
	/// </summary>
	/// <param name="tradesmanProfileId">The tradesman's profile ID.</param>
	/// <returns>A collection of reviews.</returns>
	Task<IEnumerable<Review>> GetReviewsForTradesmanAsync(Guid tradesmanProfileId);

	/// <summary>
	/// Gets the review associated with a specific booking.
	/// </summary>
	/// <param name="bookingId">The booking ID.</param>
	/// <returns>The Review object or null if not found.</returns>
	Task<Review?> GetReviewForBookingAsync(Guid bookingId);
}