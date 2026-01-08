using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

/// <summary>
/// Defines the contract for a repository handling ServiceCategory data operations.
/// </summary>
public interface IServiceCategoryRepository
{
	/// <summary>
	/// Gets a service category by its unique identifier.
	/// </summary>
	/// <param name="id">The category's unique identifier.</param>
	/// <returns>The ServiceCategory object or null if not found.</returns>
	Task<ServiceCategory?> GetByIdAsync(Guid id);

	/// <summary>
	/// Gets all service categories.
	/// </summary>
	/// <returns>A collection of all service categories.</returns>
	Task<IEnumerable<ServiceCategory>> GetAllAsync();

	/// <summary>
	/// Adds a new service category to the repository.
	/// </summary>
	/// <param name="category">The category object to add.</param>
	Task AddAsync(ServiceCategory category);

	/// <summary>
	/// Updates an existing service category.
	/// </summary>
	/// <param name="category">The category object to update.</param>
	void Update(ServiceCategory category);

	/// <summary>
	/// Deletes a service category by its unique identifier.
	/// </summary>
	/// <param name="id">The category's unique identifier.</param>
	Task DeleteAsync(Guid id);

    IQueryable<ServiceCategory> GetQueryable();
}