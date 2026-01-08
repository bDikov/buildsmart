using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;

namespace BuildSmart.Core.Application.Services;

/// <summary>
/// Handles business logic related to creating and managing reviews.
/// </summary>
public class ReviewService : IReviewService
{
	private readonly IUnitOfWork _unitOfWork;

	public ReviewService(IUnitOfWork unitOfWork)
	{
		_unitOfWork = unitOfWork;
	}

	/// <summary>
	/// Creates a new review for a completed booking.
	/// </summary>
	/// <param name="bookingId">The ID of the booking being reviewed.</param>
	/// <param name="homeownerId">The ID of the user (Homeowner) leaving the review.</param>
	/// <param name="rating">The rating (e.g., 1-5).</param>
	/// <param name="comment">The review text.</param>
	/// <returns>The newly created review.</returns>
	/// <exception cref="InvalidOperationException">Thrown if business rules are violated.</exception>
	public async Task<Review> CreateReviewAsync(Guid bookingId, Guid homeownerId, int rating, string comment)
	{
		var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);

		// --- Business Rule Checks ---
		if (booking == null)
		{
			throw new InvalidOperationException("Booking not found.");
		}
		if (booking.HomeownerId != homeownerId)
		{
			throw new InvalidOperationException("You are not authorized to review this booking.");
		}
		if (booking.Status != BookingStatusTypes.Completed)
		{
			throw new InvalidOperationException("Only completed bookings can be reviewed.");
		}

		var existingReview = await _unitOfWork.Reviews.GetByIdAsync(bookingId);
		if (existingReview != null)
		{
			throw new InvalidOperationException("This booking has already been reviewed.");
		}

		var tradesmanProfile = await _unitOfWork.TradesmanProfiles.GetByIdAsync(booking.TradesmanProfileId);
		if (tradesmanProfile == null)
		{
			throw new InvalidOperationException("Tradesman profile associated with this booking not found.");
		}

		// --- Create and Add the Review ---
		var review = new Review
		{
			BookingId = bookingId,
			Rating = rating,
			Comment = comment,
			TradesmanProfileId = booking.TradesmanProfileId,
			HomeownerId = homeownerId
		};

		await _unitOfWork.Reviews.AddAsync(review);

		// --- Update Tradesman's Average Rating ---
		// We add the new review to the profile's loaded collection
		// so the 'UpdateRating' method can calculate the new average accurately.
		tradesmanProfile.Reviews.Add(review);
		tradesmanProfile.UpdateRating();

		_unitOfWork.TradesmanProfiles.Update(tradesmanProfile);

		// --- Save all changes in one transaction ---
		await _unitOfWork.SaveChangesAsync();

		return review;
	}
}