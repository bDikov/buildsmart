using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

/// <summary>
/// Defines the contract for a repository handling Booking data operations.
/// </summary>
public interface IBookingRepository
{
	/// <summary>
	/// Gets a booking by its unique identifier.
	/// </summary>
	/// <param name="id">The booking's unique identifier.</param>
	/// <returns>The Booking object or null if not found.</returns>
	Task<Booking?> GetByIdAsync(Guid id);

	/// <summary>
	/// Adds a new booking to the repository.
	/// </summary>
	/// <param name="booking">The booking object to add.</param>
	Task AddAsync(Booking booking);

	/// <summary>
	/// Updates an existing booking.
	/// </summary>
	/// <param name="booking">The booking object to update.</param>
	void Update(Booking booking);

	/// <summary>
	/// Gets a list of all bookings for a specific tradesman.
	/// </summary>
	/// <param name="tradesmanProfileId">The tradesman's profile ID.</param>
	/// <returns>A collection of bookings.</returns>
	Task<IEnumerable<Booking>> GetBookingsForTradesmanAsync(Guid tradesmanProfileId);

	/// <summary>
	/// Gets a list of all bookings made by a specific homeowner.
	/// </summary>
	/// <param name="homeownerId">The homeowner's user ID.</param>
	/// <returns>A collection of bookings.</returns>
	Task<IEnumerable<Booking>> GetBookingsForHomeownerAsync(Guid homeownerId);
}