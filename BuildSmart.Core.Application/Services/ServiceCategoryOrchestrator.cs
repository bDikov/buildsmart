using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;

namespace BuildSmart.Core.Application.Services;

/// <summary>
/// Default implementation of IServiceCategoryOrchestrator.
/// </summary>
public class ServiceCategoryOrchestrator : IServiceCategoryOrchestrator
{
    /// <summary>
    /// Orders a queryable of service categories by their defined execution order.
    /// Order: UserType (0) -> Global (1) -> CategorySpecific (2) -> ProjectDetails (3)
    /// </summary>
    /// <param name="categories">The service categories queryable to order.</param>
    /// <returns>Ordered service categories queryable.</returns>
    public IQueryable<ServiceCategory> OrderCategories(IQueryable<ServiceCategory> categories)
    {
        if (categories == null) return Enumerable.Empty<ServiceCategory>().AsQueryable();

        return categories
            .OrderBy(c => c.Type)
            .ThenBy(c => c.Name);
    }
}
