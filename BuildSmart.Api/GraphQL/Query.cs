using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Api.GraphQL.Types;
using HotChocolate.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

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
	[UseProjection]
	[UseFiltering]
	[UseSorting]
	public IQueryable<JobPost> GetJobPostsForReview([Service] AppDbContext context)
	{
		return context.JobPosts.Where(j => j.Status == Core.Domain.Enums.JobPostStatus.WaitingForAdminReview || j.Status == Core.Domain.Enums.JobPostStatus.UnderReview);
	}

	[Authorize(Roles = new[] { "Admin" })]
	[UseProjection]
	[UseFiltering]
	[UseSorting]
	public IQueryable<Project> GetProjectsForReview([Service] AppDbContext context)
	{
		return context.Projects.Where(p =>
			p.Status == Core.Domain.Enums.ProjectStatus.UnderReview ||
			p.JobPosts.Any(j => j.Status == Core.Domain.Enums.JobPostStatus.WaitingForAdminReview || j.Status == Core.Domain.Enums.JobPostStatus.UnderReview)
		);
	}

	[Authorize(Roles = new[] { "Admin" })]
	[UseProjection]
	[UseFiltering]
	[UseSorting]
	public IQueryable<User> GetUsers([Service] AppDbContext context)
	{
		return context.Users;
	}

	[Authorize(Roles = new[] { "Tradesman" })]
	public async Task<IEnumerable<Auction>> GetAvailableAuctions(
		ClaimsPrincipal claimsPrincipal,
		[Service] IUnitOfWork unitOfWork,
		[Service] AppDbContext context)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user ID.");
		}

		// 1. Get Tradesman Profile to know their skills
		var profile = await unitOfWork.TradesmanProfiles.GetByUserIdAsync(userId);
		if (profile == null)
		{
		    return Enumerable.Empty<Auction>();
		}

		var skillCategoryIds = profile.Skills.Select(s => s.ServiceCategoryId).ToList();

		// 1.5 Get Job IDs that the tradesman has passed
		var passedJobIds = await unitOfWork.AuctionActions.GetQueryable()
		    .Where(a => a.TradesmanProfileId == profile.Id && a.ActionType == AuctionActionType.Passed)
		    .Select(a => a.JobPostId)
		    .ToListAsync();

		// 2. Find Open jobs matching skills AND NOT passed
		var jobs = await context.JobPosts
		    .Where(j => j.Status == Core.Domain.Enums.JobPostStatus.Open 
		        && skillCategoryIds.Contains(j.ServiceCategoryId)
		        && !passedJobIds.Contains(j.Id))
		    .Include(j => j.ServiceCategory)
		    .Include(j => j.Project)
		        .ThenInclude(p => p.Homeowner)
		    .Include(j => j.Bids)
		    .Include(j => j.Questions)
		    .OrderByDescending(j => j.CreatedAt)
		    .ToListAsync();

		return jobs.Select(j => new Auction
		{
		    Job = j,
		    Bids = j.Bids,
		    Questions = j.Questions
		});
		}

		[Authorize(Roles = new[] { "Tradesman", "Admin", "Homeowner" })]
		public async Task<Auction?> GetAuctionById(
		Guid jobId,
		[Service] AppDbContext context)
		{
		var job = await context.JobPosts
		    .Include(j => j.ServiceCategory)
		    .Include(j => j.Project)
		        .ThenInclude(p => p.Homeowner)
		    .Include(j => j.Bids)
		    .Include(j => j.Questions)
		    .FirstOrDefaultAsync(j => j.Id == jobId);
		if (job == null) return null;

		return new Auction
		{
		    Job = job,
		    Bids = job.Bids,
		    Questions = job.Questions
		};
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

		return await projectRepository.GetProjectsByHomeownerAsync(userId);
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

		return await notificationRepository.GetAllByUserIdAsync(userId);
	}

	[UseProjection]
	[UseFiltering]
	[UseSorting]
	public IQueryable<JobPost> GetAllJobPosts([Service] AppDbContext context) => context.JobPosts;

	[UseProjection]
	[UseFiltering]
	[UseSorting]
	public IQueryable<Project> GetAllProjects([Service] AppDbContext context) => context.Projects;
}