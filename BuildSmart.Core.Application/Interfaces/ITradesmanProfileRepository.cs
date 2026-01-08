using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

/// <summary>
/// Defines the contract for a repository handling TradesmanProfile data operations.
/// </summary>
public interface ITradesmanProfileRepository
{
	/// <summary>
	/// Gets a tradesman profile by its unique ID.
	/// </summary>
	Task<TradesmanProfile?> GetByIdAsync(Guid id);

	/// <summary>
	/// Gets a tradesman profile by the associated User ID.
	/// </summary>
	Task<TradesmanProfile?> GetByUserIdAsync(Guid userId);

	/// <summary>
	/// Gets all tradesman profiles as a queryable.
	/// </summary>
	Task<IEnumerable<TradesmanProfile>> GetAllAsync();

	/// <summary>
	/// Gets a queryable for tradesman profiles to allow further filtering/paging.
	/// This is the method GraphQL will use.
	/// </summary>
	/// <returns>An IQueryable of TradesmanProfile.</returns>
	IQueryable<TradesmanProfile> GetQueryable();

	/// <summary>
	/// Adds a new tradesman profile to the repository.
	/// </summary>
	Task AddAsync(TradesmanProfile profile);

	/// <summary>
	/// Updates an existing tradesman profile in the repository.
	/// </summary>
	void Update(TradesmanProfile profile);

	/// <summary>
	/// Deletes a tradesman profile from the repository.
	/// </summary>
	void Delete(TradesmanProfile profile);
}