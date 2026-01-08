using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Infrastructure.Persistence.Repositories;

public class BookingRepository : IBookingRepository
{
	private readonly AppDbContext _context;

	public BookingRepository(AppDbContext context)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
	}

	public async Task<Booking?> GetByIdAsync(Guid id)
	{
		return await _context.Bookings
			.Include(b => b.Homeowner)
			.Include(b => b.TradesmanProfile)
			.Include(b => b.Review)
			.FirstOrDefaultAsync(b => b.Id == id);
	}

	public async Task<IEnumerable<Booking>> GetBookingsForTradesmanAsync(Guid tradesmanProfileId)
	{
		return await _context.Bookings
			.Where(b => b.TradesmanProfileId == tradesmanProfileId)
			.Include(b => b.Homeowner)
			.Include(b => b.Review)
			.OrderByDescending(b => b.RequestedDate)
			.ToListAsync();
	}

	public async Task<IEnumerable<Booking>> GetBookingsForHomeownerAsync(Guid homeownerId)
	{
		return await _context.Bookings
			.Where(b => b.HomeownerId == homeownerId)
			.Include(b => b.TradesmanProfile)
				.ThenInclude(tp => tp.User) // Also include the Tradesman's user info
			.Include(b => b.Review)
			.OrderByDescending(b => b.RequestedDate)
			.ToListAsync();
	}

	public async Task AddAsync(Booking booking)
	{
		await _context.Bookings.AddAsync(booking);
	}

	// Added this method to match the interface
	public void Update(Booking booking)
	{
		_context.Bookings.Update(booking);
	}
}