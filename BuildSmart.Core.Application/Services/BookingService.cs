using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;

namespace BuildSmart.Core.Application.Services;

/// <summary>
/// Handles the core business logic related to bookings.
/// </summary>
public class BookingService : IBookingService
{
	private readonly IUnitOfWork _unitOfWork;

	public BookingService(IUnitOfWork unitOfWork)
	{
		_unitOfWork = unitOfWork;
	}

	/// <summary>
	/// Creates a new booking request from a homeowner to a tradesman.
	/// </summary>
	/// <param name="homeownerUserId">The ID of the user making the request.</param>
	/// <param name="tradesmanProfileId">The ID of the tradesman's profile being booked.</param>
	/// <param name="requestedDateTime">The requested date and time for the service.</param>
	/// <param name="description">A description of the job.</param>
	/// <returns>The newly created Booking object.</returns>
	/// <exception cref="ArgumentException">Thrown if the user or tradesman is not found, or if the user is not a homeowner.</exception>
	/// <exception cref="InvalidOperationException">Thrown if the tradesman is already booked for that time.</exception>
	public async Task<Booking> CreateBookingAsync(Guid homeownerUserId, Guid tradesmanProfileId, DateTime requestedDateTime, string description)
	{
        throw new NotImplementedException("Direct booking is deprecated. Bookings are now generated via the Accepted Bid (Escrow) workflow.");
	}
}