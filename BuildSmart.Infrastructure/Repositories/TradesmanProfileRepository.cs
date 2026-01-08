using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Infrastructure.Persistence.Repositories;

public class TradesmanProfileRepository : ITradesmanProfileRepository
{
	private readonly AppDbContext _context;

	public TradesmanProfileRepository(AppDbContext context)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
	}

	public async Task<TradesmanProfile?> GetByIdAsync(Guid id)
	{
		return await _context.TradesmanProfiles
			.Include(tp => tp.User)
			.Include(tp => tp.Skills)
                .ThenInclude(s => s.ServiceCategory)
			.FirstOrDefaultAsync(tp => tp.Id == id);
	}

	public async Task<TradesmanProfile?> GetByUserIdAsync(Guid userId)
	{
		return await _context.TradesmanProfiles
			.Include(tp => tp.User)
            .Include(tp => tp.Skills)
                .ThenInclude(s => s.ServiceCategory)
			.FirstOrDefaultAsync(tp => tp.UserId == userId);
	}

	public async Task<IEnumerable<TradesmanProfile>> GetAllAsync()
	{
		return await _context.TradesmanProfiles
			.Include(tp => tp.User)
            .Include(tp => tp.Skills)
                .ThenInclude(s => s.ServiceCategory)
			.ToListAsync();
	}

	public IQueryable<TradesmanProfile> GetQueryable()
	{
		// This method returns the IQueryable directly.
		// GraphQL [UsePaging] and [UseFiltering] will be applied to this.
		return _context.TradesmanProfiles
			.Include(tp => tp.User)
            .Include(tp => tp.Skills)
                .ThenInclude(s => s.ServiceCategory)
			.AsQueryable();
	}

	public async Task AddAsync(TradesmanProfile profile)
	{
		await _context.TradesmanProfiles.AddAsync(profile);
	}

	public void Update(TradesmanProfile profile)
	{
		_context.TradesmanProfiles.Update(profile);
	}

	public void Delete(TradesmanProfile profile)
	{
		_context.TradesmanProfiles.Remove(profile);
	}
}