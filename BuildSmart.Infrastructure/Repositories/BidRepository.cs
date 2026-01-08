using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Infrastructure.Persistence.Repositories;

public class BidRepository : IBidRepository
{
    private readonly AppDbContext _context;

    public BidRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Bid?> GetByIdAsync(Guid id)
    {
        return await _context.Bids
            .Include(b => b.JobPost)
            .Include(b => b.TradesmanProfile)
                .ThenInclude(tp => tp.User)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IEnumerable<Bid>> GetBidsByJobPostAsync(Guid jobPostId)
    {
        return await _context.Bids
            .Where(b => b.JobPostId == jobPostId)
            .Include(b => b.TradesmanProfile)
                .ThenInclude(tp => tp.User)
            .ToListAsync();
    }

    public async Task<IEnumerable<Bid>> GetBidsByTradesmanAsync(Guid tradesmanProfileId)
    {
        return await _context.Bids
            .Where(b => b.TradesmanProfileId == tradesmanProfileId)
            .Include(b => b.JobPost)
            .ToListAsync();
    }

    public async Task AddAsync(Bid bid)
    {
        await _context.Bids.AddAsync(bid);
    }

    public void Update(Bid bid)
    {
        _context.Bids.Update(bid);
    }
}
