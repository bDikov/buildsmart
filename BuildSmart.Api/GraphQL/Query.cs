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

	[Authorize(Roles = new[] { "Admin", "ADMIN", "admin" })]
	[UseProjection]
	[UseFiltering]
	[UseSorting]
	public IQueryable<CalculatorLead> GetCalculatorLeads([Service] AppDbContext context)
	{
		return context.CalculatorLeads.OrderByDescending(c => c.CreatedAt);
	}


	[Authorize(Roles = new[] { "Admin", "ADMIN", "admin", "Tradesman", "tradesman" })]
	public async Task<List<MediaFolderDto>> GetMediaFolders(
		Guid? parentId,
		[Service] IUnifiedMediaService mediaService,
		CancellationToken cancellationToken)
	{
		var folders = await mediaService.GetFoldersAsync(parentId, cancellationToken);
		return folders.Select(f => new MediaFolderDto
		{
			Id = f.Id,
			ParentId = f.ParentId,
			Name = f.Name,
			Slug = f.Slug,
			FullPath = f.FullPath,
			IsSystem = f.IsSystem,
			ItemCount = f.Assets.Count,
			SubFolderCount = f.SubFolders.Count,
			CreatedAt = f.CreatedAt,
			UpdatedAt = f.UpdatedAt
		}).ToList();
	}

	[Authorize(Roles = new[] { "Admin", "ADMIN", "admin", "Tradesman", "tradesman" })]
	public async Task<MediaFolderDto?> GetMediaFolderByPath(
		string fullPath,
		[Service] IUnifiedMediaService mediaService,
		CancellationToken cancellationToken)
	{
		var folder = await mediaService.GetFolderByPathAsync(fullPath, cancellationToken);
		if (folder == null) return null;

		return new MediaFolderDto
		{
			Id = folder.Id,
			ParentId = folder.ParentId,
			Name = folder.Name,
			Slug = folder.Slug,
			FullPath = folder.FullPath,
			IsSystem = folder.IsSystem,
			ItemCount = folder.Assets.Count,
			SubFolderCount = folder.SubFolders.Count,
			CreatedAt = folder.CreatedAt,
			UpdatedAt = folder.UpdatedAt
		};
	}

	[Authorize(Roles = new[] { "Admin", "ADMIN", "admin", "Tradesman", "tradesman" })]
	public async Task<MediaAssetsResultDto> GetMediaAssets(
		Guid? folderId,
		string? mediaType,
		string? searchTerm,
		int? skip,
		int? take,
		[Service] IUnifiedMediaService mediaService,
		[Service] AppDbContext context,
		CancellationToken cancellationToken)
	{
		var (items, totalCount) = await mediaService.GetAssetsAsync(
			folderId,
			mediaType,
			searchTerm,
			skip ?? 0,
			take ?? 50,
			cancellationToken);

		// If MediaAssets table is empty (e.g. initial setup before migration/upload), fetch from legacy records
		if (totalCount == 0 && !folderId.HasValue && string.IsNullOrEmpty(searchTerm))
		{
			var legacyList = await GetMediaLibraryAssets(context, cancellationToken);
			if (legacyList.Count > 0)
			{
				var filtered = legacyList.AsEnumerable();
				if (!string.IsNullOrWhiteSpace(mediaType) && !string.Equals(mediaType, "all", StringComparison.OrdinalIgnoreCase))
				{
					filtered = filtered.Where(x => string.Equals(x.MediaType, mediaType, StringComparison.OrdinalIgnoreCase));
				}
				var paged = filtered.Skip(skip ?? 0).Take(take ?? 50).ToList();
				return new MediaAssetsResultDto
				{
					TotalCount = legacyList.Count,
					Items = paged
				};
			}
		}

		return new MediaAssetsResultDto
		{
			TotalCount = totalCount,
			Items = items.Select(a => new MediaAssetDto
			{
				Id = a.Id,
				FolderId = a.FolderId,
				FolderPath = a.Folder?.FullPath,
				FileName = a.FileName,
				R2Key = a.R2Key,
				PublicUrl = a.PublicUrl,
				ThumbnailUrl = a.ThumbnailUrl,
				MediaType = a.MediaType,
				ContentType = a.ContentType,
				SizeBytes = a.SizeBytes,
				Width = a.Width,
				Height = a.Height,
				DurationSeconds = a.DurationSeconds,
				AltTextBg = a.AltTextBg,
				AltTextEn = a.AltTextEn,
				CreatedAt = a.CreatedAt,
				UpdatedAt = a.UpdatedAt
			}).ToList()
		};
	}

	public async Task<List<MediaAssetDto>> GetMediaLibraryAssets([Service] AppDbContext context, CancellationToken cancellationToken)
	{
		var directAssets = await context.MediaAssets
			.Include(a => a.Folder)
			.OrderByDescending(a => a.CreatedAt)
			.Take(200)
			.ToListAsync(cancellationToken);

		if (directAssets.Count > 0)
		{
			return directAssets.Select(a => new MediaAssetDto
			{
				Id = a.Id,
				FolderId = a.FolderId,
				FolderPath = a.Folder?.FullPath,
				FileName = a.FileName,
				R2Key = a.R2Key,
				PublicUrl = a.PublicUrl,
				ThumbnailUrl = a.ThumbnailUrl,
				MediaType = a.MediaType,
				ContentType = a.ContentType,
				SizeBytes = a.SizeBytes,
				Width = a.Width,
				Height = a.Height,
				DurationSeconds = a.DurationSeconds,
				AltTextBg = a.AltTextBg,
				AltTextEn = a.AltTextEn,
				CreatedAt = a.CreatedAt,
				UpdatedAt = a.UpdatedAt
			}).ToList();
		}

		var list = new List<MediaAssetDto>();

		var feedMedia = await context.TradesmanMedia.AsNoTracking().ToListAsync(cancellationToken);
		foreach (var m in feedMedia)
		{
			if (!string.IsNullOrEmpty(m.VideoUrl))
			{
				list.Add(new MediaAssetDto
				{
					Id = m.Id,
					PublicUrl = m.VideoUrl,
					ThumbnailUrl = m.ThumbnailUrl ?? m.ImageUrl ?? string.Empty,
					MediaType = m.Type == Core.Domain.Enums.MediaType.Video ? "video" : "image",
					FileName = "Feed Video Asset",
					CreatedAt = m.CreatedAt
				});
			}
			if (!string.IsNullOrEmpty(m.ImageUrl) && m.ImageUrl != m.VideoUrl)
			{
				list.Add(new MediaAssetDto
				{
					Id = Guid.NewGuid(),
					PublicUrl = m.ImageUrl,
					ThumbnailUrl = m.ImageUrl,
					MediaType = "image",
					FileName = "Feed Image Asset",
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
					PublicUrl = lp.HeroImageUrl,
					ThumbnailUrl = lp.HeroImageUrl,
					MediaType = "image",
					FileName = $"Hero Banner (/{lp.Slug})",
					CreatedAt = lp.CreatedAt
				});
			}
			if (!string.IsNullOrEmpty(lp.HeroVideoUrl))
			{
				list.Add(new MediaAssetDto
				{
					Id = Guid.NewGuid(),
					PublicUrl = lp.HeroVideoUrl,
					ThumbnailUrl = string.Empty,
					MediaType = "video",
					FileName = $"Hero Video (/{lp.Slug})",
					CreatedAt = lp.CreatedAt
				});
			}
			if (!string.IsNullOrEmpty(lp.MediaGalleryJson))
			{
				try
				{
					var galleryItems = System.Text.Json.JsonSerializer.Deserialize<List<MediaGalleryItemDto>>(
						lp.MediaGalleryJson,
						new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					if (galleryItems != null)
					{
						foreach (var g in galleryItems)
						{
							if (!string.IsNullOrWhiteSpace(g.Url))
							{
								var isVid = string.Equals(g.Type, "video", StringComparison.OrdinalIgnoreCase) ||
								            g.Url.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
								            g.Url.EndsWith(".webm", StringComparison.OrdinalIgnoreCase);
								var title = !string.IsNullOrWhiteSpace(g.CaptionBg)
									? g.CaptionBg
									: (!string.IsNullOrWhiteSpace(g.CaptionEn) ? g.CaptionEn : (isVid ? $"Video Slide (/{lp.Slug})" : $"Image Slide (/{lp.Slug})"));
								list.Add(new MediaAssetDto
								{
									Id = Guid.NewGuid(),
									PublicUrl = g.Url,
									ThumbnailUrl = isVid ? string.Empty : g.Url,
									MediaType = isVid ? "video" : "image",
									FileName = title,
									CreatedAt = lp.CreatedAt
								});
							}
						}
					}
				}
				catch { }
			}
		}

		return list.GroupBy(x => x.PublicUrl).Select(g => g.First()).OrderByDescending(x => x.CreatedAt).ToList();
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