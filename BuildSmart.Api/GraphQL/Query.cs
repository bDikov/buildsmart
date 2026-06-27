using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Api.GraphQL.Types;
using HotChocolate.Authorization;
using HotChocolate.Types;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Api.GraphQL;

public class Query
{
	[UseOffsetPaging(IncludeTotalCount = true, DefaultPageSize = 3, MaxPageSize = 10)]
	[UseProjection]
	[UseFiltering]
	[UseSorting]
	public IQueryable<TradesmanMedia> GetFeedMedia([Service] AppDbContext context)
	{
		return context.TradesmanMedia.Where(m => m.IsActive).OrderByDescending(m => m.CreatedAt);
	}

	[UseProjection]
	[UseFiltering]
	[UseSorting]
	public IQueryable<TradesmanProfile> GetTradesmanProfiles([Service] ITradesmanProfileRepository tradesmanProfileRepository)
	{
		return tradesmanProfileRepository.GetQueryable();
	}

	public async Task<User?> GetCurrentUser(
		ClaimsPrincipal claimsPrincipal,
		[Service] IUserRepository userRepository)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub");

		if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
		{
			return null;
		}

		return await userRepository.GetByIdAsync(userId);
	}

	[UseProjection]
	[UseFiltering]
	[UseSorting]
	public IQueryable<ServiceCategory> GetServiceCategories([Service] IServiceCategoryRepository categoryRepository)
	{
		return categoryRepository.GetQueryable().Where(c => c.Status == Core.Domain.Enums.CategoryStatus.Active);
	}

	[Authorize(Roles = new[] { "Admin" })]
	[UseProjection]
	[UseFiltering]
	[UseSorting]
	public IQueryable<ServiceCategory> GetAllServiceCategories([Service] IServiceCategoryRepository categoryRepository)
	{
		return categoryRepository.GetQueryable();
	}

	[Authorize(Roles = new[] { "Admin" })]
	public IQueryable<ServiceSku> GetServiceSkusByCategory([Service] AppDbContext context, Guid categoryId)
	{
		return context.ServiceSkus.Where(s => s.ServiceCategoryId == categoryId);
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

    [Authorize]
    [UseProjection]
    public IQueryable<AiCalculation> GetAiCalculation([Service] AppDbContext context, Guid projectId, Guid categoryId)
    {
        return context.AiCalculations.Where(a => a.ProjectId == projectId && a.ServiceCategoryId == categoryId);
    }

    [Authorize]
    [UseProjection]
    public IQueryable<AiCalculation> GetAiCalculationByJob([Service] AppDbContext context, Guid jobId)
    {
        var jobPost = context.JobPosts.FirstOrDefault(j => j.Id == jobId);
        if (jobPost == null) return Enumerable.Empty<AiCalculation>().AsQueryable();

        return context.AiCalculations.Where(a => a.ProjectId == jobPost.ProjectId && a.ServiceCategoryId == jobPost.ServiceCategoryId);
    }

	[Authorize(Roles = new[] { "Tradesman" })]
	public async Task<IEnumerable<Auction>> GetAvailableAuctions(
		ClaimsPrincipal claimsPrincipal,
		[Service] IUnitOfWork unitOfWork,
		[Service] AppDbContext context)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
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
			.OrderByDescending(j => j.CreatedAt)
			.ToListAsync();
		return jobs.Select(j => new Auction
		{
			Job = j,
			Bids = j.Bids,
			Questions = j.Questions
		});
	}

	[Authorize(Roles = new[] { "Tradesman" })]
	public async Task<IEnumerable<Auction>> GetPassedAuctions(
		ClaimsPrincipal claimsPrincipal,
		[Service] IUnitOfWork unitOfWork,
		[Service] AppDbContext context)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user ID.");
		}

		var profile = await unitOfWork.TradesmanProfiles.GetByUserIdAsync(userId);
		if (profile == null) return Enumerable.Empty<Auction>();

		// Find jobs that have been PASSED by this tradesman
		var passedJobIds = await unitOfWork.AuctionActions.GetQueryable()
			.Where(a => a.TradesmanProfileId == profile.Id && a.ActionType == AuctionActionType.Passed)
			.Select(a => a.JobPostId)
			.ToListAsync();

		var jobs = await context.JobPosts
			.Where(j => passedJobIds.Contains(j.Id))
			.Include(j => j.ServiceCategory)
			.Include(j => j.Project)
				.ThenInclude(p => p.Homeowner)
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
			.Include(j => j.Feedbacks)
				.ThenInclude(f => f.Author)
			.Include(j => j.Feedbacks)
				.ThenInclude(f => f.Replies)
					.ThenInclude(r => r.Author)
			.Include(j => j.Questions)
				.ThenInclude(q => q.Author)
			.Include(j => j.Questions)
				.ThenInclude(q => q.TradesmanProfile)
					.ThenInclude(tp => tp.User)
			.FirstOrDefaultAsync(j => j.Id == jobId);
		if (job == null) return null;

		return new Auction
		{
			Job = job,
			Bids = job.Bids,
			Questions = job.Questions
		};
	}

	[Authorize]
	public async Task<IEnumerable<Project>> GetMyProjects(
	ClaimsPrincipal claimsPrincipal,
	[Service] IProjectRepository projectRepository)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user ID in token.");
		}

		return await projectRepository.GetProjectsByHomeownerAsync(userId);
	}

	[Authorize]
	public async Task<Project?> GetProjectById(
		Guid projectId,
		ClaimsPrincipal claimsPrincipal,
		[Service] AppDbContext context)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user ID.");
		}

		var project = await context.Projects
			.AsSplitQuery()
			.Include(p => p.Homeowner)
			.Include(p => p.JobPosts)
				.ThenInclude(j => j.ServiceCategory)
			.Include(p => p.JobPosts)
				.ThenInclude(j => j.Feedbacks)
					.ThenInclude(f => f.Author)
			.Include(p => p.JobPosts)
				.ThenInclude(j => j.Feedbacks)
					.ThenInclude(f => f.Replies)
						.ThenInclude(r => r.Author)
			.Include(p => p.JobPosts)
				.ThenInclude(j => j.Questions)
					.ThenInclude(q => q.Author)
			.Include(p => p.JobPosts)
				.ThenInclude(j => j.Questions)
					.ThenInclude(q => q.TradesmanProfile)
						.ThenInclude(tp => tp.User)
			.FirstOrDefaultAsync(p => p.Id == projectId);

		if (project == null) return null;

		var isAdmin = claimsPrincipal.IsInRole("Admin");
		if (!isAdmin && project.HomeownerId != userId)
		{
			throw new GraphQLException("You are not authorized to view this project.");
		}

		foreach (var jp in project.JobPosts)
		{
			foreach (var bid in jp.Bids)
			{
				await context.Entry(bid).Collection(b => b.BidItems).Query()
					.Include(bi => bi.JobTask)
						.ThenInclude(jt => jt.AcceptanceCriteria)
					.LoadAsync();
			}
		}

		return project;
	}

	[Authorize]
	public async Task<IEnumerable<Notification>> GetMyNotifications(
		ClaimsPrincipal claimsPrincipal,
		[Service] INotificationRepository notificationRepository)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user ID in token.");
		}

		return await notificationRepository.GetAllByUserIdAsync(userId);
	}

	[Authorize]
	public async Task<JobPostQuestion?> GetJobPostQuestionById(
		Guid id,
		[Service] IJobPostQuestionRepository repository)
	{
		return await repository.GetByIdAsync(id);
	}

	[UseProjection]
	[UseFiltering]
	[UseSorting]
	public IQueryable<JobPost> GetAllJobPosts([Service] AppDbContext context) => context.JobPosts;

	public async Task<Bid?> GetBidDetailsById(Guid bidId, [Service] AppDbContext context)
	{
		return await context.Bids
			.Include(b => b.TradesmanProfile)
				.ThenInclude(tp => tp.User)
			.Include(b => b.BidItems)
				.ThenInclude(bi => bi.JobTask)
					.ThenInclude(jt => jt.AcceptanceCriteria)
			.FirstOrDefaultAsync(b => b.Id == bidId);
	}

	[UseProjection]
	[UseFiltering]
	[UseSorting]
	public IQueryable<Project> GetAllProjects([Service] AppDbContext context) => context.Projects;

	[Authorize(Roles = new[] { "Tradesman" })]
	[UseProjection]
	[UseFiltering]
	[UseSorting]
	public IQueryable<Booking> GetMyActiveBookings(
		ClaimsPrincipal claimsPrincipal,
		[Service] AppDbContext context)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		var tradesmanProfile = context.TradesmanProfiles.FirstOrDefault(tp => tp.UserId == userId);
		if (tradesmanProfile == null)
		{
			return Enumerable.Empty<Booking>().AsQueryable();
		}

		return context.Bookings
			.Where(b => b.TradesmanProfileId == tradesmanProfile.Id)
			.OrderByDescending(b => b.CreatedAt);
	}

	[Authorize]
	public async Task<IEnumerable<ProjectMessage>> GetProjectMessages(
		Guid projectId,
		int offset,
		int limit,
		ClaimsPrincipal claimsPrincipal,
		[Service] IProjectChatService chatService)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		return await chatService.GetProjectMessagesAsync(projectId, userId, offset, limit);
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<IEnumerable<ProjectChatSummary>> GetActiveSupportChats(
		[Service] AppDbContext context,
		[Service] IUserPresenceService presenceService)
	{
		var activeChats = await context.Projects
			.Include(p => p.Homeowner)
			.Where(p => context.ProjectMessages.Any(m => m.ProjectId == p.Id && m.SenderId == p.HomeownerId))
			.Select(p => new ProjectChatSummary
			{
				ProjectId = p.Id,
				ProjectTitle = p.Title,
				HomeownerName = $"{p.Homeowner.FirstName} {p.Homeowner.LastName}",
				HomeownerId = p.HomeownerId,
				LatestMessageText = context.ProjectMessages
					.Where(m => m.ProjectId == p.Id)
					.OrderByDescending(m => m.CreatedAt)
					.Select(m => m.MessageText)
					.FirstOrDefault() ?? string.Empty,
				LatestMessageTime = context.ProjectMessages
					.Where(m => m.ProjectId == p.Id)
					.OrderByDescending(m => m.CreatedAt)
					.Select(m => (DateTime?)m.CreatedAt)
					.FirstOrDefault()
			})
			.OrderByDescending(c => c.LatestMessageTime)
			.ToListAsync();

		foreach (var chat in activeChats)
		{
			chat.IsHomeownerOnline = presenceService.IsUserOnline(chat.HomeownerId.ToString());
		}

		return activeChats;
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<IEnumerable<Question>> GetQuestions([Service] IQuestionManagementService questionService, CancellationToken cancellationToken)
	{
		return await questionService.GetAllQuestionsAsync(cancellationToken);
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<IEnumerable<Formula>> GetFormulas([Service] IQuestionManagementService questionService, CancellationToken cancellationToken)
	{
		return await questionService.GetAllFormulasAsync(cancellationToken);
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<QuestionGraphResponse> GetQuestionGraph([Service] IQuestionManagementService questionService, CancellationToken cancellationToken)
	{
		var (nodes, edges) = await questionService.GetGraphDataAsync(cancellationToken);
		return new QuestionGraphResponse
		{
			Nodes = nodes,
			Edges = edges
		};
	}
}

public class QuestionGraphResponse
{
	public IEnumerable<BuildSmart.Core.Application.DTOs.GraphNodeDto> Nodes { get; set; } = Array.Empty<BuildSmart.Core.Application.DTOs.GraphNodeDto>();
	public IEnumerable<BuildSmart.Core.Application.DTOs.GraphEdgeDto> Edges { get; set; } = Array.Empty<BuildSmart.Core.Application.DTOs.GraphEdgeDto>();
}

public class ProjectChatSummary
{
	public Guid ProjectId { get; set; }
	public string ProjectTitle { get; set; } = string.Empty;
	public string HomeownerName { get; set; } = string.Empty;
	public Guid HomeownerId { get; set; }
	public string LatestMessageText { get; set; } = string.Empty;
	public DateTime? LatestMessageTime { get; set; }
	public bool IsHomeownerOnline { get; set; }
}