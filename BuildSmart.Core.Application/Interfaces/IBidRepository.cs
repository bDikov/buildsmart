using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IBidRepository
{
	Task<Bid?> GetByIdAsync(Guid id);

	Task<IEnumerable<Bid>> GetBidsByJobPostAsync(Guid jobPostId);

	Task<IEnumerable<Bid>> GetBidsByTradesmanAsync(Guid tradesmanProfileId);

	Task AddAsync(Bid bid);

	void Update(Bid bid);

	IQueryable<Bid> GetQueryable();
}