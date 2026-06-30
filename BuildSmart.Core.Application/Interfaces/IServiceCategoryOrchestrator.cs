using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

/// <summary>
/// Orchestrates service categories by validating, ordering, or filtering them.
/// </summary>
public interface IServiceCategoryOrchestrator
{
    /// <summary>
    /// Orders a queryable of service categories by their defined execution order.
    /// </summary>
    /// <param name="categories">The service categories queryable to order.</param>
    /// <returns>Ordered service categories queryable.</returns>
    IQueryable<ServiceCategory> OrderCategories(IQueryable<ServiceCategory> categories);
}
