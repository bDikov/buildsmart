using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Api.GraphQL.Types;
using BuildSmart.Api.DTOs;
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
	public IQueryable<BlogPost> GetBlogPosts([Service] AppDbContext context)
	{
		return context.BlogPosts.Where(b => b.IsPublished).OrderByDescending(b => b.PublishedAt);
	}

	public async Task<BlogPost?> GetBlogPostBySlug(string slug, [Service] AppDbContext context, CancellationToken cancellationToken)
	{
		return await context.BlogPosts.AsNoTracking().FirstOrDefaultAsync(b => b.Slug == slug, cancellationToken);
	}

	[UseProjection]
	[UseFiltering]
	[UseSorting]
	public IQueryable<LandingPageContent> GetLandingPages([Service] AppDbContext context)
	{
		return context.LandingPages.OrderByDescending(l => l.UpdatedAt);
	}

	public async Task<LandingPageContent?> GetLandingPageBySlug(string slug, [Service] AppDbContext context, CancellationToken cancellationToken)
	{
		return await context.LandingPages.AsNoTracking().FirstOrDefaultAsync(l => l.Slug == slug, cancellationToken);
	}

	public async Task<List<MediaAssetDto>> GetMediaLibraryAssets([Service] AppDbContext context, CancellationToken cancellationToken)
	{
		var list = new List<MediaAssetDto>();

		var feedMedia = await context.TradesmanMedia.AsNoTracking().ToListAsync(cancellationToken);
		foreach (var m in feedMedia)
		{
			if (!string.IsNullOrEmpty(m.VideoUrl))
			{
				list.Add(new MediaAssetDto
				{
					Id = m.Id,
					Url = m.VideoUrl,
					ThumbnailUrl = m.ThumbnailUrl ?? m.ImageUrl ?? string.Empty,
					Type = m.Type == Core.Domain.Enums.MediaType.Video ? "video" : "image",
					Title = "Feed Video Asset",
					CreatedAt = m.CreatedAt
				});
			}
			if (!string.IsNullOrEmpty(m.ImageUrl) && m.ImageUrl != m.VideoUrl)
			{
				list.Add(new MediaAssetDto
				{
					Id = Guid.NewGuid(),
					Url = m.ImageUrl,
					ThumbnailUrl = m.ImageUrl,
					Type = "image",
					Title = "Feed Image Asset",
					CreatedAt = m.CreatedAt
				});
			}
		}

		var landingPages = await context.LandingPages.AsNoTracking().ToListAsync(cancellationToken);
		foreach (var lp in landingPages)
		{
			if (!string.IsNullOrEmpty(lp.HeroImageUrl))
			{
				list.Add(new MediaAssetDto
				{
					Id = Guid.NewGuid(),
					Url = lp.HeroImageUrl,
					ThumbnailUrl = lp.HeroImageUrl,
					Type = "image",
					Title = $"Hero Banner (/{lp.Slug})",
					CreatedAt = lp.CreatedAt
				});
			}
			if (!string.IsNullOrEmpty(lp.HeroVideoUrl))
			{
				list.Add(new MediaAssetDto
				{
					Id = Guid.NewGuid(),
					Url = lp.HeroVideoUrl,
					ThumbnailUrl = string.Empty,
					Type = "video",
					Title = $"Hero Video (/{lp.Slug})",
					CreatedAt = lp.CreatedAt
				});
			}
		}

		return list.GroupBy(x => x.Url).Select(g => g.First()).OrderByDescending(x => x.CreatedAt).ToList();
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
	public IQueryable<ServiceCategory> GetServiceCategories(
		[Service] IServiceCategoryRepository categoryRepository,
		[Service] IServiceCategoryOrchestrator orchestrator)
	{
		var query = categoryRepository.GetQueryable().Where(c => c.Status == Core.Domain.Enums.CategoryStatus.Active);
		return orchestrator.OrderCategories(query);
	}

	[Authorize(Roles = new[] { "Admin" })]
	[UseProjection]
	[UseFiltering]
	[UseSorting]
	public IQueryable<ServiceCategory> GetAllServiceCategories(
		[Service] IServiceCategoryRepository categoryRepository,
		[Service] IServiceCategoryOrchestrator orchestrator)
	{
		var query = categoryRepository.GetQueryable();
		return orchestrator.OrderCategories(query);
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

	[Authorize(Roles = new[] { "Tradesman", "TRADESMAN", "tradesman" })]
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

	[Authorize(Roles = new[] { "Tradesman", "TRADESMAN", "tradesman" })]
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

	[Authorize(Roles = new[] { "Tradesman", "TRADESMAN", "tradesman", "Admin", "ADMIN", "admin", "Homeowner", "HOMEOWNER", "homeowner" })]
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
		[Service] AppDbContext context)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user ID in token.");
		}

		var isAdmin = claimsPrincipal.IsInRole("Admin");
		if (isAdmin)
		{
			return await context.Projects
				.Include(p => p.Homeowner)
				.Include(p => p.JobPosts)
					.ThenInclude(j => j.ServiceCategory)
				.OrderByDescending(p => p.CreatedAt)
				.ToListAsync();
		}

		return await context.Projects
			.Include(p => p.Homeowner)
			.Include(p => p.JobPosts)
				.ThenInclude(j => j.ServiceCategory)
			.Where(p => p.HomeownerId == userId 
				|| p.JobPosts.Any(j => j.AssignedTradesmanId == userId)
				|| context.CategoryTradesmanAssignments.Any(a => a.ProjectId == p.Id && a.TradesmanId == userId)
				|| context.Bids.Any(b => b.JobPost.ProjectId == p.Id && b.TradesmanProfile.UserId == userId)
				|| context.Bookings.Any(b => b.JobPost.ProjectId == p.Id && b.TradesmanProfile.UserId == userId))
			.OrderByDescending(p => p.CreatedAt)
			.ToListAsync();
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
			var isAssigned = project.JobPosts.Any(j => j.AssignedTradesmanId == userId)
				|| await context.CategoryTradesmanAssignments.AnyAsync(a => a.ProjectId == projectId && a.TradesmanId == userId)
				|| await context.Bids.AnyAsync(b => b.JobPost.ProjectId == projectId && b.TradesmanProfile.UserId == userId)
				|| await context.Bookings.AnyAsync(b => b.JobPost.ProjectId == projectId && b.TradesmanProfile.UserId == userId);

			if (!isAssigned)
			{
				throw new GraphQLException("You are not authorized to view this project.");
			}
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
			.Where(p => context.ProjectMessages.Any(m => m.ProjectId == p.Id))
			.Select(p => new ProjectChatSummary
			{
				ProjectId = p.Id,
				ProjectTitle = p.Title,
				HomeownerName = $"{p.Homeowner.FirstName} {p.Homeowner.LastName}",
				HomeownerEmail = p.Homeowner.Email ?? string.Empty,
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
	public async Task<IEnumerable<Question>> GetQuestions(
		[Service] IQuestionManagementService questionService, 
		[Service] AppDbContext context,
		CancellationToken cancellationToken)
	{
		var questions = (await questionService.GetAllQuestionsAsync(cancellationToken)).ToList();
		var translations = await context.LocalizationResources
			.AsNoTracking()
			.Where(r => r.Culture == "en")
			.ToListAsync(cancellationToken);

		foreach (var q in questions)
		{
			q.EnglishText = translations.FirstOrDefault(t => t.Key == q.Text)?.Value;
			if (!string.IsNullOrEmpty(q.HintText))
			{
				q.EnglishHint = translations.FirstOrDefault(t => t.Key == q.HintText)?.Value;
			}
		}

		return questions;
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

	public async Task<IEnumerable<LocalizationResource>> GetLocalizationStrings(
		string culture,
		[Service] AppDbContext context)
	{
		return await context.LocalizationResources
			.AsNoTracking()
			.Where(r => r.Culture == culture)
			.ToListAsync();
	}

	public async Task<IEnumerable<LocalizationResource>> GetAllLocalizationResources(
		[Service] AppDbContext context)
	{
		return await context.LocalizationResources
			.AsNoTracking()
			.OrderBy(r => r.Key)
			.ThenBy(r => r.Culture)
			.ToListAsync();
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
	public string HomeownerEmail { get; set; } = string.Empty;
	public Guid HomeownerId { get; set; }
	public string LatestMessageText { get; set; } = string.Empty;
	public DateTime? LatestMessageTime { get; set; }
	public bool IsHomeownerOnline { get; set; }
}