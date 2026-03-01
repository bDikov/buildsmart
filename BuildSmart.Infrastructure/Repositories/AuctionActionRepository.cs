using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Infrastructure.Persistence.Repositories;

public class AuctionActionRepository : IAuctionActionRepository
{
    private readonly AppDbContext _context;

    public AuctionActionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(TradesmanAuctionAction action)
    {
        await _context.TradesmanAuctionActions.AddAsync(action);
    }

    public async Task<IEnumerable<TradesmanAuctionAction>> GetActionsByTradesmanAsync(Guid tradesmanProfileId)
    {
        return await _context.TradesmanAuctionActions
            .Where(a => a.TradesmanProfileId == tradesmanProfileId)
            .ToListAsync();
    }

    public IQueryable<TradesmanAuctionAction> GetQueryable()
    {
        return _context.TradesmanAuctionActions.AsQueryable();
    }

    public void Delete(TradesmanAuctionAction action)
    {
        _context.TradesmanAuctionActions.Remove(action);
    }
}
