using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IReviewService
{
	Task<Review> CreateReviewAsync(Guid bookingId, Guid homeownerId, int rating, string comment);
}