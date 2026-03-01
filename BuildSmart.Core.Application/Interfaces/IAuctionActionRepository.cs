using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IAuctionActionRepository
{
    Task AddAsync(TradesmanAuctionAction action);
    Task<IEnumerable<TradesmanAuctionAction>> GetActionsByTradesmanAsync(Guid tradesmanProfileId);
    IQueryable<TradesmanAuctionAction> GetQueryable();
    void Delete(TradesmanAuctionAction action);
}
