using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using HotChocolate.Authorization;
using System.Security.Claims;

namespace BuildSmart.Api.GraphQL;

public class Query
{
	public IQueryable<TradesmanProfile> GetTradesmanProfiles([Service] ITradesmanProfileRepository tradesmanProfileRepository)
	{
		return tradesmanProfileRepository.GetQueryable();
	}

	public async Task<User?> GetCurrentUser(
		ClaimsPrincipal claimsPrincipal,
		[Service] IUserRepository userRepository)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);

		if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
		{
			return null;
		}

		return await userRepository.GetByIdAsync(userId);
	}

	public IQueryable<ServiceCategory> GetServiceCategories([Service] IServiceCategoryRepository categoryRepository)
	{
		return categoryRepository.GetQueryable().Where(c => c.Status == Core.Domain.Enums.CategoryStatus.Active);
	}

	[Authorize(Roles = new[] { "Admin" })]
	public IQueryable<ServiceCategory> GetAllServiceCategories([Service] IServiceCategoryRepository categoryRepository)
	{
		return categoryRepository.GetQueryable();
	}
}