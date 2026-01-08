using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Infrastructure.Persistence.Repositories;

public class ReviewRepository : IReviewRepository
{
	private readonly AppDbContext _context;

	public ReviewRepository(AppDbContext context)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
	}

	public async Task<Review?> GetByIdAsync(Guid id)
	{
		return await _context.Reviews
			.Include(r => r.Booking)
			.FirstOrDefaultAsync(r => r.Id == id);
	}

	public async Task<IEnumerable<Review>> GetReviewsForTradesmanAsync(Guid tradesmanProfileId)
	{
		return await _context.Reviews
			.Include(r => r.Booking)
			.Where(r => r.Booking.TradesmanProfileId == tradesmanProfileId)
			.OrderByDescending(r => r.CreatedAt)
			.ToListAsync();
	}

	public async Task AddAsync(Review review)
	{
		await _context.Reviews.AddAsync(review);
	}

	public async Task<Review?> GetReviewForBookingAsync(Guid bookingId)
	{
		return await _context.Reviews
			.FirstOrDefaultAsync(r => r.BookingId == bookingId);
	}
}