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
		var homeowner = await _unitOfWork.Users.GetByIdAsync(homeownerUserId);
		if (homeowner == null || homeowner.Role != UserRoleTypes.Homeowner)
		{
			throw new ArgumentException("Invalid homeowner ID.", nameof(homeownerUserId));
		}

		var tradesmanProfile = await _unitOfWork.TradesmanProfiles.GetByIdAsync(tradesmanProfileId);
		if (tradesmanProfile == null)
		{
			throw new ArgumentException("Invalid tradesman profile ID.", nameof(tradesmanProfileId));
		}

		var existingBookings = await _unitOfWork.Bookings.GetBookingsForTradesmanAsync(tradesmanProfile.Id);
		if (existingBookings.Any(b => b.RequestedDate.Date == requestedDateTime.Date && b.Status != BookingStatusTypes.Cancelled && b.Status != BookingStatusTypes.Completed))
		{
			throw new InvalidOperationException("This tradesman is already booked for the selected date.");
		}

		var newBooking = new Booking
		{
			HomeownerId = homeownerUserId,
			TradesmanProfileId = tradesmanProfileId,
			RequestedDate = requestedDateTime,
			JobDescription = description,
		};

		await _unitOfWork.Bookings.AddAsync(newBooking);
		await _unitOfWork.SaveChangesAsync();

		return newBooking;
	}
}