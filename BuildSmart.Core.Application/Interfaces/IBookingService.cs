using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IBookingService
{
	Task<Booking> CreateBookingAsync(Guid homeownerUserId, Guid tradesmanProfileId, DateTime requestedDateTime, string description);
}