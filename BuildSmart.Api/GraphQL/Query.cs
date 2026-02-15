using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
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

	[Authorize(Roles = new[] { "Admin" })]
	public IQueryable<JobPost> GetJobPostsForReview([Service] AppDbContext context)
	{
		return context.JobPosts.Where(j => j.Status == Core.Domain.Enums.JobPostStatus.WaitingForAdminReview);
	}

	[Authorize(Roles = new[] { "Homeowner" })]
	public async Task<IEnumerable<Project>> GetMyProjects(
		ClaimsPrincipal claimsPrincipal,
		[Service] IProjectRepository projectRepository)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user ID in token.");
		}

		var projects = await projectRepository.GetProjectsByHomeownerAsync(userId);
		Console.WriteLine($"[GetMyProjects] Found {projects.Count()} projects for user {userId}.");
		foreach (var p in projects)
		{
			Console.WriteLine($" - Project {p.Id}: {p.Title}, Jobs: {p.JobPosts?.Count ?? 0}");
		}
		return projects;
	}

    [Authorize]
    public async Task<IEnumerable<Notification>> GetMyNotifications(
        ClaimsPrincipal claimsPrincipal,
        [Service] INotificationRepository notificationRepository)
    {
        var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new GraphQLException("Invalid user ID in token.");
        }

        return await notificationRepository.GetUnreadByUserIdAsync(userId);
    }

	[UseProjection]
	[UseFiltering]
	[UseSorting]
	public IQueryable<JobPost> GetAllJobPosts([Service] AppDbContext context) => context.JobPosts;

	        [UseProjection]

	        [UseFiltering]

	        [UseSorting]

	        public IQueryable<Project> GetAllProjects([Service] AppDbContext context) => context.Projects;}