using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Application.Services;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Core.Domain.ValueObjects;
using BuildSmart.Api.DTOs;
using HotChocolate.Authorization;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Hangfire;
using BuildSmart.Infrastructure.Persistence;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using BuildSmart.Api.Hubs;

namespace BuildSmart.Api.GraphQL;

public class Mutation
{
	[Authorize(Roles = new[] { "Tradesman" })]
	public async Task<PortfolioEntry> AddPortfolioEntry(
		string title,
		string? description,
		IFile file,
		ClaimsPrincipal claimsPrincipal,
		[Service] IUnitOfWork unitOfWork,
		[Service] IMultimediaStorageService storageService)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		var user = await unitOfWork.Users.GetByIdAsync(userId);
		if (user?.TradesmanProfile == null)
		{
			throw new GraphQLException("Tradesman profile not found.");
		}

		// Save the file
		string imageUrl;
		using (var stream = file.OpenReadStream())
		{
			imageUrl = await storageService.SaveFileAsync(stream, file.Name, file.ContentType);
		}

		var entry = new PortfolioEntry
		{
			Title = title,
			Description = description,
			ImageUrl = imageUrl,
			TradesmanProfileId = user.TradesmanProfile.Id
		};

		user.TradesmanProfile.PortfolioEntries.Add(entry);
		await unitOfWork.SaveChangesAsync();

		return entry;
	}

	[Authorize(Roles = new[] { "Tradesman" })]
	public async Task<Certification> AddCertification(
		string title,
		string? description,
		IFile file,
		DateTime issuedAt,
		DateTime? expiresAt,
		ClaimsPrincipal claimsPrincipal,
		[Service] IUnitOfWork unitOfWork,
		[Service] IMultimediaStorageService storageService)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		var user = await unitOfWork.Users.GetByIdAsync(userId);
		if (user?.TradesmanProfile == null)
		{
			throw new GraphQLException("Tradesman profile not found.");
		}

		// Save the file
		string documentUrl;
		using (var stream = file.OpenReadStream())
		{
			documentUrl = await storageService.SaveFileAsync(stream, file.Name, file.ContentType);
		}

		var cert = new Certification
		{
			Title = title,
			Description = description,
			DocumentUrl = documentUrl,
			IssuedAt = issuedAt,
			ExpiresAt = expiresAt,
			TradesmanProfileId = user.TradesmanProfile.Id
		};

		user.TradesmanProfile.Certifications.Add(cert);
		await unitOfWork.SaveChangesAsync();

		return cert;
	}

	[Authorize(Roles = new[] { "Tradesman" })]
	public async Task<bool> UpdateVideoIntroduction(
		IFile file,
		ClaimsPrincipal claimsPrincipal,
		[Service] IUnitOfWork unitOfWork,
		[Service] IMultimediaStorageService storageService)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		var user = await unitOfWork.Users.GetByIdAsync(userId);
		if (user?.TradesmanProfile == null)
		{
			throw new GraphQLException("Tradesman profile not found.");
		}

		// Delete old video if it exists
		if (!string.IsNullOrEmpty(user.TradesmanProfile.VideoIntroductionUrl))
		{
			await storageService.DeleteFileAsync(user.TradesmanProfile.VideoIntroductionUrl);
		}

		// Save the new file
		string videoUrl;
		using (var stream = file.OpenReadStream())
		{
			string? contentType = file.ContentType;
			videoUrl = await storageService.SaveFileAsync(stream, file.Name, contentType);
		}

		user.TradesmanProfile.VideoIntroductionUrl = videoUrl;
		await unitOfWork.SaveChangesAsync();

		return true;
	}

	[Authorize(Roles = new[] { "Tradesman" })]
	public async Task<bool> RestoreAuction(
		Guid jobId,
		ClaimsPrincipal claimsPrincipal,
		[Service] IUnitOfWork unitOfWork)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user ID.");
		}

		var profile = await unitOfWork.TradesmanProfiles.GetByUserIdAsync(userId);
		if (profile == null) throw new GraphQLException("Tradesman profile not found.");

		// Find and remove the "Passed" action
		var action = await unitOfWork.AuctionActions.GetQueryable()
			.FirstOrDefaultAsync(a => a.TradesmanProfileId == profile.Id
				&& a.JobPostId == jobId
				&& a.ActionType == AuctionActionType.Passed);

		if (action != null)
		{
			// Note: I might need to add a Delete method to IAuctionActionRepository if it doesn't exist
			// For now, I'll use the DbContext directly or ensure the repository has it.
			unitOfWork.AuctionActions.Delete(action);
			await unitOfWork.SaveChangesAsync();
			return true;
		}

		return false;
	}

	public async Task<int> MigratePasswords([Service] DataMigrationService dataMigrationService)
	{
		return await dataMigrationService.HashExistingPasswordsAsync();
	}

	public async Task<string> Login(
		string email,
		string password,
		[Service] IUnitOfWork unitOfWork,
		[Service] IConfiguration configuration)
	{
		var user = await unitOfWork.Users.GetByEmailAsync(email);

		if (user == null)
		{
			throw new GraphQLException(new Error("Invalid credentials", "AUTH_INVALID_CREDENTIALS"));
		}

		if (string.IsNullOrEmpty(user.HashedPassword))
		{
			throw new GraphQLException(new Error("This account was created using an external provider. Please use Google or Apple to log in.", "AUTH_EXTERNAL_PROVIDER"));
		}

		if (!BCrypt.Net.BCrypt.Verify(password, user.HashedPassword))
		{
			throw new GraphQLException(new Error("Invalid credentials", "AUTH_INVALID_CREDENTIALS"));
		}

		if (!user.IsEmailVerified)
		{
			throw new GraphQLException(new Error("Please verify your email address before logging in.", "AUTH_EMAIL_NOT_VERIFIED"));
		}

		var issuer = configuration["Jwt:Issuer"];
		var audience = configuration["Jwt:Audience"];
		var key = Encoding.ASCII.GetBytes(configuration["Jwt:Key"]!);

		var tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(new[]
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(ClaimTypes.Role, user.Role.ToString())
			}),
			Expires = DateTime.UtcNow.AddMinutes(30),
			Issuer = issuer,
			Audience = audience,
			SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
		};

		var tokenHandler = new JwtSecurityTokenHandler();
		var token = tokenHandler.CreateToken(tokenDescriptor);
		return tokenHandler.WriteToken(token);
	}

	public async Task<bool> VerifyEmail(
		string email,
		string code,
		[Service] IAuthService authService)
	{
		return await authService.VerifyEmailAsync(email, code);
	}

	public async Task<bool> ResendVerificationCode(
		string email,
		[Service] IAuthService authService)
	{
		return await authService.ResendVerificationCodeAsync(email);
	}

	public async Task<User> RegisterUser(
		string firstName,
		string lastName,
		string email,
		string password,
		string? phoneNumber,
		[Service] IAuthService authService)
	{
		return await authService.RegisterUserAsync(firstName, lastName, email, password, phoneNumber);
	}

	[Authorize]
	public async Task<User> UpdateUserLanguage(
		string languageCode,
		ClaimsPrincipal claimsPrincipal,
		[Service] IUnitOfWork unitOfWork)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		var user = await unitOfWork.Users.GetByIdAsync(userId);
		if (user == null) throw new GraphQLException("User not found.");

		user.PreferredLanguage = languageCode;
		unitOfWork.Users.Update(user);
		await unitOfWork.SaveChangesAsync();

		return user;
	}

	[Authorize]
	public async Task<User> UpdateUserTheme(
		string theme,
		ClaimsPrincipal claimsPrincipal,
		[Service] IUnitOfWork unitOfWork)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		var user = await unitOfWork.Users.GetByIdAsync(userId);
		if (user == null) throw new GraphQLException("User not found.");

		user.PreferredTheme = theme;
		unitOfWork.Users.Update(user);
		await unitOfWork.SaveChangesAsync();

		return user;
	}

	[Authorize]
	public async Task<User> UpdateUserEmailNotifications(
		bool emailOnOfferReady,
		bool emailOnNewChatMessage,
		ClaimsPrincipal claimsPrincipal,
		[Service] IUnitOfWork unitOfWork)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		var user = await unitOfWork.Users.GetByIdAsync(userId);
		if (user == null) throw new GraphQLException("User not found.");

		user.EmailOnOfferReady = emailOnOfferReady;
		user.EmailOnNewChatMessage = emailOnNewChatMessage;
		unitOfWork.Users.Update(user);
		await unitOfWork.SaveChangesAsync();

		return user;
	}

	[Authorize]
	public async Task<User> UpdateUserProfile(
		Guid userId,
		string firstName,
		string lastName,
		string? bio,
		string? location,
		string? profilePictureUrl,
		string? phoneNumber,
		string? email,
		[Service] IAuthService authService)
	{
		return await authService.UpdateUserProfileAsync(userId, firstName, lastName, bio, location, profilePictureUrl, phoneNumber, email);
	}

	public async Task<Review> SubmitReview(
		Guid bookingId,
		Guid homeownerId,
		int rating,
		string comment,
		[Service] IReviewService reviewService)
	{
		return await reviewService.CreateReviewAsync(
			bookingId,
			homeownerId,
			rating,
			comment
		);
	}

	public async Task<ServiceCategory> CreateServiceCategory(
		string name,
		string? description,
		string templateStructure,
		[Service] IUnitOfWork unitOfWork)
	{
		// Simple implementation directly in Mutation for now, ideally moved to a Service
		var category = new ServiceCategory
		{
			Name = name,
			Description = description,
			TemplateStructure = templateStructure,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};

		await unitOfWork.ServiceCategories.AddAsync(category);
		await unitOfWork.SaveChangesAsync();

		return category;
	}

	public async Task<Project> CreateProject(
		Guid homeownerId,
		string title,
		string description,
		string? languageCode,
		[Service] IJobPostService jobPostService)
	{
		return await jobPostService.CreateProjectAsync(homeownerId, title, description, languageCode);
	}

	[Authorize]
	public async Task<Project> UpdateProjectDetails(
		Guid projectId,
		string title,
		string description,
		int? lastVisitedStep,
		ClaimsPrincipal claimsPrincipal,
		[Service] IUnitOfWork unitOfWork)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Unauthorized");
		}

		var project = await unitOfWork.Projects.GetByIdAsync(projectId);
		if (project == null) throw new GraphQLException("Project not found.");

		if (project.HomeownerId != userId)
		{
			throw new GraphQLException("Unauthorized");
		}

		project.Title = title;
		project.Description = description;
		if (lastVisitedStep.HasValue)
		{
			project.LastVisitedStep = lastVisitedStep.Value;
		}
		project.UpdatedAt = DateTime.UtcNow;

		unitOfWork.Projects.Update(project);
		await unitOfWork.SaveChangesAsync();

		return project;
	}

	[Authorize]
	public async Task<bool> UpdateProjectLocation(
		Guid projectId,
		string location,
		ClaimsPrincipal claimsPrincipal,
		[Service] IUnitOfWork unitOfWork)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Unauthorized");
		}

		var project = await unitOfWork.Projects.GetByIdAsync(projectId);
		if (project == null) throw new GraphQLException("Project not found.");

		if (project.HomeownerId != userId)
		{
			throw new GraphQLException("Unauthorized");
		}

		var jobs = await unitOfWork.JobPosts.GetJobsByProjectIdAsync(projectId);
		foreach (var job in jobs)
		{
			job.Location = location;
			unitOfWork.JobPosts.Update(job);
		}

		await unitOfWork.SaveChangesAsync();
		return true;
	}


	[Authorize]
	public async Task<bool> DeleteProject(
		Guid projectId,
		ClaimsPrincipal claimsPrincipal,
		[Service] IUnitOfWork unitOfWork)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		var project = await unitOfWork.Projects.GetByIdAsync(projectId);
		if (project == null)
		{
			throw new GraphQLException("Project not found.");
		}

		// Security Check: Ensure the user owns the project or is an Admin
		var isAdmin = claimsPrincipal.IsInRole(UserRoleTypes.Admin.ToString());
		if (!isAdmin && project.HomeownerId != userId)
		{
			throw new GraphQLException(new Error("You do not have permission to delete this project.", "AUTH_NOT_AUTHORIZED"));
		}

		// Business Rule: Homeowners cannot delete active projects
		if (!isAdmin && project.Status == Core.Domain.Enums.ProjectStatus.Active)
		{
			throw new GraphQLException(new Error("Homeowners cannot delete a project while it is Active.", "PROJECT_IS_ACTIVE"));
		}

		await unitOfWork.Notifications.DeleteProjectNotificationsAsync(projectId);
		await unitOfWork.Projects.DeleteAsync(projectId);
		await unitOfWork.SaveChangesAsync();
		return true;
	}

	[Authorize]
	public async Task<bool> DeleteJobPost(
		Guid jobPostId,
		ClaimsPrincipal claimsPrincipal,
		[Service] IUnitOfWork unitOfWork)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		var jobPost = await unitOfWork.JobPosts.GetByIdAsync(jobPostId);

		if (jobPost == null)
		{
			throw new GraphQLException("Job post not found.");
		}

		// Security Check: Ensure the user owns the project or is an Admin
		var isAdmin = claimsPrincipal.IsInRole(UserRoleTypes.Admin.ToString());
		if (!isAdmin && jobPost.Project.HomeownerId != userId)
		{
			throw new GraphQLException(new Error("You do not have permission to delete this job.", "AUTH_NOT_AUTHORIZED"));
		}

		// Business Rule: Homeowners cannot delete jobs from active projects
		if (!isAdmin && jobPost.Project.Status == Core.Domain.Enums.ProjectStatus.Active)
		{
			throw new GraphQLException(new Error("Homeowners cannot delete a job while its parent project is Active.", "PROJECT_IS_ACTIVE"));
		}

		if (jobPost.Project != null)
		{
			jobPost.Project.MasterOfferPdf = null;
			jobPost.Project.GeneralSummary = null;
			unitOfWork.Projects.Update(jobPost.Project);
		}

		unitOfWork.JobPosts.Delete(jobPost);
		await unitOfWork.SaveChangesAsync();
		return true;
	}

	public async Task<JobPost> AddJobToProject(
		Guid projectId,
		Guid categoryId,
		string title,
		string jobDetailsJson,
		string? location,
		decimal? estimatedSubtotal,
		string currency,
		List<string> imageUrls,
		DateTime? preferredSiteVisitDate,
		[Service] IJobPostService jobPostService)
	{
		Amount? budget = estimatedSubtotal.HasValue
			? Amount.Create(currency, estimatedSubtotal.Value)
			: null;

		// Fallback: If location is not provided, use a default or fetch from project/homeowner?
		// For now, if null, we pass "Remote" or similar to avoid DB crash, but ideally UI sends it.
		var finalLocation = location ?? "Not Specified";

		return await jobPostService.AddJobToProjectAsync(
			projectId,
			categoryId,
			title,
			jobDetailsJson,
			finalLocation,
			budget,
			imageUrls,
			preferredSiteVisitDate
		);
	}

	public async Task<bool> SaveJobPostDraft(
		Guid jobPostId,
		string jobDetailsJson,
		string? description,
		string? location,
		decimal? estimatedSubtotal,
		string currency,
		[Service] IJobPostService jobPostService)
	{
		Amount? budget = estimatedSubtotal.HasValue
			? Amount.Create(currency, estimatedSubtotal.Value)
			: null;

		await jobPostService.SaveDraftAsync(jobPostId, jobDetailsJson, description, location, budget);
		return true;
	}

	[Authorize(Roles = new[] { "Homeowner", "Admin" })]
	public async Task<bool> UpdateJobTasks(
		UpdateJobTasksInput input,
		[Service] IJobPostService jobPostService)
	{
		var tasks = input.Tasks.Select(t => (t.Id, t.Title, t.Description, t.SequenceOrder, t.Criteria.Select(c => (c.Id, c.Description))));
		await jobPostService.UpdateJobTasksAsync(input.JobPostId, tasks);
		return true;
	}

	public async Task<bool> SubmitJobPost(
		Guid jobPostId,
		[Service] IJobPostService jobPostService)
	{
		await jobPostService.SubmitJobPostAsync(jobPostId);
		return true;
	}

	public async Task<Bid> SubmitBid(
		SubmitBidInput input,
		[Service] IJobPostService jobPostService)
	{
		var bidItems = input.BidItems.Select(bi => (bi.JobTaskId, bi.PriceSubtotal, bi.Comment));

		return await jobPostService.SubmitBidAsync(
			input.TradesmanProfileId, 
			input.JobPostId, 
			input.Currency, 
			input.Comment, 
			input.EarliestStartDate, 
			input.LatestStartDate, 
			input.EstimatedDurationDays, 
			bidItems);
	}

	[Authorize(Roles = new[] { "Tradesman" })]
	public async Task<bool> PassAuction(
		Guid tradesmanProfileId,
		Guid jobPostId,
		[Service] IJobPostService jobPostService)
	{
		await jobPostService.PassAuctionAsync(tradesmanProfileId, jobPostId);
		return true;
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<User> UpdateUserRoleAndCategories(
		Guid userId,
		UserRoleTypes newRole,
		List<Guid>? serviceCategoryIds,
		[Service] IAuthService authService)
	{
		return await authService.UpdateUserRoleAndCategoriesAsync(userId, newRole, serviceCategoryIds);
	}

	[Authorize(Roles = new[] { "Tradesman" })]
	public async Task<JobPostQuestion> AskJobQuestion(
		Guid tradesmanProfileId,
		Guid jobPostId,
		string questionText,
		[Service] IJobPostService jobPostService)
	{
		return await jobPostService.AskJobQuestionAsync(tradesmanProfileId, jobPostId, questionText);
	}

	[Authorize(Roles = new[] { "Homeowner" })]
	public async Task<JobPostQuestion> AnswerJobQuestion(
		Guid questionId,
		string answerText,
		[Service] IJobPostService jobPostService)
	{
		return await jobPostService.AnswerJobQuestionAsync(questionId, answerText);
	}

	[Authorize]
	public async Task<JobPostQuestion> EditJobQuestion(
		Guid questionId,
		string newText,
		ClaimsPrincipal claimsPrincipal,
		[Service] IJobPostService jobPostService)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		return await jobPostService.EditJobQuestionAsync(questionId, userId, newText);
	}

	[Authorize]
	public async Task<JobPostFeedback> EditJobFeedback(
		Guid feedbackId,
		string newText,
		ClaimsPrincipal claimsPrincipal,
		[Service] IJobPostService jobPostService)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		return await jobPostService.EditJobFeedbackAsync(feedbackId, userId, newText);
	}

	[Authorize(Roles = new[] { "Homeowner" })]
	public async Task<JobPostQuestion> EditJobAnswer(
		Guid questionId,
		string newAnswer,
		ClaimsPrincipal claimsPrincipal,
		[Service] IUnitOfWork unitOfWork,
		[Service] IJobPostService jobPostService)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		var user = await unitOfWork.Users.GetByIdAsync(userId);
		if (user?.HomeownerProfile == null)
		{
			throw new GraphQLException("Homeowner profile not found.");
		}

		return await jobPostService.EditJobAnswerAsync(questionId, user.HomeownerProfile.Id, newAnswer);
	}

	[Authorize]
	public async Task<JobPostQuestion> ReplyToJobQuestion(
		Guid parentQuestionId,
		string replyText,
		ClaimsPrincipal claimsPrincipal,
		[Service] IJobPostService jobPostService)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		return await jobPostService.ReplyToQuestionAsync(parentQuestionId, userId, replyText);
	}

	[Authorize(Roles = new[] { "Homeowner" })]
	public async Task<Booking> AcceptBid(
		Guid bidId,
		ClaimsPrincipal claimsPrincipal,
		[Service] IPaymentService paymentService)
	{
		try
		{
			var userIdClaim = claimsPrincipal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
			if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
			{
				throw new GraphQLException("Invalid user credentials.");
			}

			return await paymentService.AcceptBidAsync(userId, bidId);
		}
		catch (Exception ex)
		{
			// Unroll inner exceptions so we can see the true EF Core crash!
			var realError = ex.InnerException?.Message ?? ex.Message;
			throw new GraphQLException($"AcceptBid crashed: {realError} | {ex.StackTrace}");
		}
	}

	[Authorize(Roles = new[] { "Homeowner" })]
	public async Task<bool> ApproveMilestone(
		Guid milestonePaymentId,
		ClaimsPrincipal claimsPrincipal,
		[Service] IPaymentService paymentService)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		await paymentService.ApproveMilestoneAsync(userId, milestonePaymentId);
		return true;
	}

	[Authorize]
	public async Task<bool> SubmitJobForScopeGeneration(
		Guid jobPostId,
		[Service] IJobPostService jobPostService)
	{
		await jobPostService.SubmitJobForScopeGenerationAsync(jobPostId);
		return true;
	}

	[Authorize]
	public async Task<bool> ApproveJobScope(
		Guid jobPostId,
		string finalScope,
		[Service] IJobPostService jobPostService)
	{
		await jobPostService.ApproveJobScopeAsync(jobPostId, finalScope);
		return true;
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<bool> AdminReviewJobScope(
		Guid jobPostId,
		bool approved,
		string? feedback,
		ClaimsPrincipal claimsPrincipal,
		[Service] IJobPostService jobPostService)
	{
		var userIdClaim = claimsPrincipal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
		Guid? adminId = (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var id)) ? id : null;

		await jobPostService.AdminReviewJobScopeAsync(jobPostId, approved, feedback, adminId);
		return true;
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<bool> AdminRegenerateOffer(
		Guid projectId,
		[Service] IUnitOfWork unitOfWork,
		[Service] BuildSmart.Core.Application.Interfaces.IScopeGenerationQueue scopeQueue)
	{
		var project = await unitOfWork.Projects.GetByIdAsync(projectId);
		if (project == null) throw new GraphQLException("Project not found.");

		project.MasterOfferPdf = null;
		project.GeneralSummary = null;
		unitOfWork.Projects.Update(project);

		foreach (var job in project.JobPosts)
		{
			// Reset status to GeneratingScope so UI shows it's loading, and it allows background retry.
			job.SubmitForScopeGeneration();
			unitOfWork.JobPosts.Update(job);
			
			await scopeQueue.QueuePricingUpdateAsync(job.Id, CancellationToken.None);
		}
		await unitOfWork.SaveChangesAsync();
		
		return true;
	}

	[Authorize]
	public async Task<JobPostFeedback> AddJobFeedback(
		Guid jobPostId,
		string text,
		ClaimsPrincipal claimsPrincipal,
		[Service] IJobPostService jobPostService)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		return await jobPostService.AddFeedbackAsync(jobPostId, userId, text);
	}

	[Authorize]
	public async Task<JobPostFeedback> ReplyToJobFeedback(
		Guid parentFeedbackId,
		string text,
		ClaimsPrincipal claimsPrincipal,
		[Service] IJobPostService jobPostService)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		return await jobPostService.ReplyToFeedbackAsync(parentFeedbackId, userId, text);
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<JobPostFeedback> ResolveJobFeedback(
		Guid feedbackId,
		[Service] IJobPostService jobPostService)
	{
		return await jobPostService.ResolveFeedbackAsync(feedbackId);
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<bool> AddAdminJobQuestion(
		Guid jobPostId,
		string questionText,
		string type,
		bool isRequired,
		List<string>? options,
		[Service] IJobPostService jobPostService)
	{
		return await jobPostService.AddAdminQuestionAsync(jobPostId, questionText, type, isRequired, options);
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<ServiceCategory> UpdateCategoryStatus(
			Guid categoryId,
			CategoryStatus newStatus,
			[Service] IUnitOfWork unitOfWork)
	{
		var category = await unitOfWork.ServiceCategories.GetByIdAsync(categoryId)
			?? throw new GraphQLException("Category not found.");

		category.Status = newStatus;
		unitOfWork.ServiceCategories.Update(category);
		await unitOfWork.SaveChangesAsync();
		return category;
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<ServiceCategory> SaveCategory(
		Guid? id,
		string name,
		string? description,
		bool isGlobal,
		string templateStructure,
		CategoryStatus? status,
		string? englishName,
		[Service] IUnitOfWork unitOfWork,
		[Service] AppDbContext context)
	{
		ServiceCategory category;
		if (id.HasValue && id.Value != Guid.Empty)
		{
			// Update existing
			category = await unitOfWork.ServiceCategories.GetByIdAsync(id.Value) ?? throw new GraphQLException("Category not found.");
			category.Name = name;
			category.Description = description;
			category.IsGlobal = isGlobal;
			category.TemplateStructure = templateStructure;
			category.EnglishName = englishName?.Trim();
			category.EnglishDescription = description;
			if (status.HasValue)
			{
				category.Status = status.Value;
			}
			unitOfWork.ServiceCategories.Update(category);
		}
		else
		{
			// Create new
			category = new ServiceCategory
			{
				Id = Guid.NewGuid(),
				Name = name,
				Description = description,
				IsGlobal = isGlobal,
				TemplateStructure = templateStructure,
				Status = status ?? CategoryStatus.Draft,
				EnglishName = englishName?.Trim(),
				EnglishDescription = description
			};
			await unitOfWork.ServiceCategories.AddAsync(category);
		}

		await unitOfWork.SaveChangesAsync();
		return category;
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<bool> DeleteServiceCategory(
		Guid id,
		[Service] IUnitOfWork unitOfWork,
		[Service] AppDbContext context)
	{
		var category = await unitOfWork.ServiceCategories.GetByIdAsync(id);
		if (category == null) return false;

		await unitOfWork.ServiceCategories.DeleteAsync(id);
		await unitOfWork.SaveChangesAsync();
		return true;
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<ServiceSku> CreateServiceSku(
		Guid categoryId,
		string skuCode,
		string name,
		string description,
		decimal basePrice,
		string unitType,
		[Service] IUnitOfWork unitOfWork,
		[Service] AppDbContext context,
		string? calculationFormula = null,
		string? englishName = null,
		string? englishDescription = null)
	{
		var sku = new ServiceSku
		{
			Id = Guid.NewGuid(),
			ServiceCategoryId = categoryId,
			SkuCode = skuCode,
			Name = name,
			Description = description,
			BasePrice = basePrice,
			UnitType = unitType,
			CalculationFormula = calculationFormula ?? "",
			EnglishName = englishName?.Trim(),
			EnglishDescription = englishDescription ?? "",
			EnglishUnitType = GetEnglishUnitType(unitType),
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};

		await unitOfWork.ServiceSkus.AddAsync(sku);
		await unitOfWork.SaveChangesAsync();
		return sku;
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<ServiceSku> UpdateServiceSku(
		Guid id,
		string skuCode,
		string name,
		string description,
		decimal basePrice,
		string unitType,
		[Service] IUnitOfWork unitOfWork,
		[Service] AppDbContext context,
		string? calculationFormula = null,
		string? englishName = null,
		string? englishDescription = null)
	{
		var sku = await unitOfWork.ServiceSkus.GetByIdAsync(id)
			?? throw new GraphQLException("SKU not found.");

		sku.SkuCode = skuCode;
		sku.Name = name;
		sku.Description = description;
		sku.BasePrice = basePrice;
		sku.UnitType = unitType;
		sku.CalculationFormula = calculationFormula ?? "";
		sku.EnglishName = englishName?.Trim();
		sku.EnglishDescription = englishDescription ?? "";
		sku.EnglishUnitType = GetEnglishUnitType(unitType);
		sku.UpdatedAt = DateTime.UtcNow;

		unitOfWork.ServiceSkus.Update(sku);
		await unitOfWork.SaveChangesAsync();
		return sku;
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<bool> DeleteServiceSku(
		Guid id,
		[Service] IUnitOfWork unitOfWork)
	{
		var sku = await unitOfWork.ServiceSkus.GetByIdAsync(id)
			?? throw new GraphQLException("SKU not found.");

		unitOfWork.ServiceSkus.Delete(sku);
		await unitOfWork.SaveChangesAsync();
		return true;
	}

	[Authorize]
	public async Task<bool> DeleteAllNotifications(
		ClaimsPrincipal claimsPrincipal,
		[Service] IUnitOfWork unitOfWork)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		await unitOfWork.Notifications.DeleteAllByUserIdAsync(userId);
		await unitOfWork.SaveChangesAsync();
		return true;
	}

	[Authorize]
	public async Task<bool> MarkNotificationAsRead(
		Guid notificationId,
		[Service] IUnitOfWork unitOfWork)
	{
		await unitOfWork.Notifications.MarkAsReadAsync(notificationId);
		await unitOfWork.SaveChangesAsync();
		return true;
	}

	[Authorize]
	public async Task<bool> MarkProjectNotificationsAsRead(
		Guid projectId,
		ClaimsPrincipal claimsPrincipal,
		[Service] IUnitOfWork unitOfWork)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		await unitOfWork.Notifications.MarkProjectNotificationsAsReadAsync(userId, projectId);
		await unitOfWork.SaveChangesAsync();
		return true;
	}

	[Authorize(Roles = new[] { "Admin" })]
	public string RequestVideoUploadUrl(
		string fileName,
		string contentType,
		[Service] IMediaService mediaService)
	{
		return mediaService.GeneratePreSignedUploadUrl(fileName, contentType, TimeSpan.FromMinutes(15));
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<TradesmanMedia> ConfirmVideoUpload(
		Guid tradesmanUserId,
		string videoUrl,
		string? imageUrl,
        BuildSmart.Core.Domain.Enums.MediaType type,
        Guid? serviceCategoryId,
		[Service] IUnitOfWork unitOfWork,
		[Service] Microsoft.Extensions.Configuration.IConfiguration config,
		[Service] Hangfire.IBackgroundJobClient backgroundJobs)
	{
		var profile = await unitOfWork.TradesmanProfiles.GetByUserIdAsync(tradesmanUserId)
			?? throw new GraphQLException("Tradesman profile not found.");

		// Remap the internal S3 URL to the Public CDN URL if configured
		var publicBaseUrl = config["CloudflareR2:PublicUrl"];
		var bucketName = config["CloudflareR2:BucketName"];
		
		videoUrl = RemapUrl(videoUrl, publicBaseUrl, bucketName) ?? videoUrl;
		imageUrl = RemapUrl(imageUrl, publicBaseUrl, bucketName);

		var media = new TradesmanMedia
		{
			Id = Guid.NewGuid(),
			TradesmanId = profile.Id, 
			VideoUrl = type == BuildSmart.Core.Domain.Enums.MediaType.Video ? videoUrl : string.Empty,
			MobileVideoUrl = null, // Set to null; processed asynchronously
            ImageUrl = type == BuildSmart.Core.Domain.Enums.MediaType.Picture ? videoUrl : imageUrl,
            ThumbnailUrl = type == BuildSmart.Core.Domain.Enums.MediaType.Picture ? videoUrl : imageUrl,
            Type = type,
            ServiceCategoryId = serviceCategoryId,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow,
			IsActive = true
		};

		await unitOfWork.TradesmanProfiles.AddMediaAsync(media);
		await unitOfWork.SaveChangesAsync();

		if (type == BuildSmart.Core.Domain.Enums.MediaType.Video)
		{
			backgroundJobs.Enqueue<BuildSmart.Api.Workers.VideoProcessingJob>(job => job.ProcessVideoAsync(media.Id));
		}

		return media;
	}

	private string? RemapUrl(string? url, string? publicBaseUrl, string? bucketName)
	{
		if (string.IsNullOrEmpty(url)) return url;
		if (!string.IsNullOrEmpty(publicBaseUrl) && Uri.TryCreate(url, UriKind.Absolute, out var parsedUri))
		{
			var path = parsedUri.AbsolutePath;
			if (!string.IsNullOrEmpty(bucketName) && path.StartsWith($"/{bucketName}/"))
			{
				path = path.Substring($"/{bucketName}".Length);
			}
			return $"{publicBaseUrl.TrimEnd('/')}{path}";
		}
		return url;
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<TradesmanMedia> ToggleTradesmanMediaStatus(
		Guid mediaId,
		bool isActive,
		[Service] BuildSmart.Infrastructure.Persistence.AppDbContext dbContext)
	{
		if (dbContext == null) throw new GraphQLException("Database context not found.");

		var media = await dbContext.TradesmanMedia.FirstOrDefaultAsync(m => m.Id == mediaId)
			?? throw new GraphQLException("Media not found.");

		media.IsActive = isActive;
		media.UpdatedAt = DateTime.UtcNow;

		dbContext.TradesmanMedia.Update(media);
		await dbContext.SaveChangesAsync();

		return media;
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<TradesmanMedia> UpdateTradesmanMediaCategory(
		Guid mediaId,
		Guid? categoryId,
		[Service] BuildSmart.Infrastructure.Persistence.AppDbContext dbContext)
	{
		if (dbContext == null) throw new GraphQLException("Database context not found.");

		var media = await dbContext.TradesmanMedia.FirstOrDefaultAsync(m => m.Id == mediaId)
			?? throw new GraphQLException("Media not found.");

		media.ServiceCategoryId = categoryId;
		media.UpdatedAt = DateTime.UtcNow;

		dbContext.TradesmanMedia.Update(media);
		await dbContext.SaveChangesAsync();

		return media;
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<bool> DeleteTradesmanMedia(
		Guid mediaId,
		[Service] BuildSmart.Infrastructure.Persistence.AppDbContext dbContext)
	{
		if (dbContext == null) throw new GraphQLException("Database context not found.");

		var media = await dbContext.TradesmanMedia.FirstOrDefaultAsync(m => m.Id == mediaId)
			?? throw new GraphQLException("Media not found.");

		dbContext.TradesmanMedia.Remove(media);
		await dbContext.SaveChangesAsync();

		return true;
	}

	[Authorize]
	public async Task<ProjectMessage> SendProjectMessage(
		Guid projectId,
		string messageText,
		ClaimsPrincipal claimsPrincipal,
		[Service] IProjectChatService chatService)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier) ?? claimsPrincipal.FindFirst("sub") ?? claimsPrincipal.FindFirst("nameid");
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}
		return await chatService.SendMessageAsync(projectId, userId, messageText);
	}

	public async Task<AnonymousChatPayload> StartAnonymousSupportChat(
		[Service] IUnitOfWork unitOfWork,
		[Service] IConfiguration configuration)
	{
		var guestGuid = Guid.NewGuid();
		var guestEmail = $"guest_{guestGuid:N}@buildsmart.guest";

		var user = new User
		{
			Email = guestEmail,
			FirstName = "Guest",
			LastName = "User",
			Role = UserRoleTypes.Homeowner,
			HashedPassword = null,
			IsEmailVerified = false
		};
		await unitOfWork.Users.AddAsync(user);
		await unitOfWork.SaveChangesAsync();

		var supportProject = new Project
		{
			Title = "Support Chat",
			Description = $"Anonymous GDPR support session for guest user {guestGuid}.",
			HomeownerId = user.Id,
			LanguageCode = "bg"
		};
		await unitOfWork.Projects.AddAsync(supportProject);
		await unitOfWork.SaveChangesAsync();

		var issuer = configuration["Jwt:Issuer"];
		var audience = configuration["Jwt:Audience"];
		var key = Encoding.ASCII.GetBytes(configuration["Jwt:Key"]!);

		var tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(new[]
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(ClaimTypes.Role, user.Role.ToString())
			}),
			Expires = DateTime.UtcNow.AddDays(7),
			Issuer = issuer,
			Audience = audience,
			SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
		};

		var tokenHandler = new JwtSecurityTokenHandler();
		var token = tokenHandler.CreateToken(tokenDescriptor);
		var jwtToken = tokenHandler.WriteToken(token);

		return new AnonymousChatPayload
		{
			Token = jwtToken,
			ProjectId = supportProject.Id
		};
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<Question> CreateQuestion(
		string questionCode,
		string text,
		string type,
		bool isRequired,
		string? optionsJson,
		string? hintText,
		Guid? serviceCategoryId,
		Guid? parentQuestionId,
		int displayOrder,
		string? visibilityCondition,
		[Service] IQuestionManagementService questionService,
		[Service] AppDbContext context,
		CancellationToken cancellationToken,
		string? englishText = null,
		string? englishHint = null)
	{
		var question = new Question
		{
			QuestionCode = questionCode,
			Text = text,
			Type = type,
			IsRequired = isRequired,
			OptionsJson = optionsJson,
			HintText = hintText,
			ServiceCategoryId = serviceCategoryId,
			ParentQuestionId = parentQuestionId,
			DisplayOrder = displayOrder,
			VisibilityCondition = visibilityCondition,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		var result = await questionService.CreateQuestionAsync(question, cancellationToken);

		// Add/Update english text translation
		if (!string.IsNullOrWhiteSpace(englishText))
		{
			var resource = await context.LocalizationResources
				.FirstOrDefaultAsync(r => r.Key == text && r.Culture == "en", cancellationToken);
			if (resource != null)
			{
				resource.Value = englishText.Trim();
				resource.UpdatedAt = DateTime.UtcNow;
				context.LocalizationResources.Update(resource);
			}
			else
			{
				await context.LocalizationResources.AddAsync(new LocalizationResource
				{
					Id = Guid.NewGuid(),
					Key = text,
					Culture = "en",
					Value = englishText.Trim(),
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				}, cancellationToken);
			}
		}

		// Add/Update english hint translation
		if (!string.IsNullOrWhiteSpace(hintText) && !string.IsNullOrWhiteSpace(englishHint))
		{
			var resource = await context.LocalizationResources
				.FirstOrDefaultAsync(r => r.Key == hintText && r.Culture == "en", cancellationToken);
			if (resource != null)
			{
				resource.Value = englishHint.Trim();
				resource.UpdatedAt = DateTime.UtcNow;
				context.LocalizationResources.Update(resource);
			}
			else
			{
				await context.LocalizationResources.AddAsync(new LocalizationResource
				{
					Id = Guid.NewGuid(),
					Key = hintText,
					Culture = "en",
					Value = englishHint.Trim(),
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				}, cancellationToken);
			}
		}

		await context.SaveChangesAsync(cancellationToken);
		return result;
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<Question> UpdateQuestion(
		Guid id,
		string questionCode,
		string text,
		string type,
		bool isRequired,
		string? optionsJson,
		string? hintText,
		Guid? serviceCategoryId,
		Guid? parentQuestionId,
		int displayOrder,
		string? visibilityCondition,
		[Service] IQuestionManagementService questionService,
		[Service] AppDbContext context,
		CancellationToken cancellationToken,
		string? englishText = null,
		string? englishHint = null)
	{
		var question = await questionService.GetQuestionByIdAsync(id, cancellationToken);
		if (question == null)
		{
			throw new GraphQLException($"Question with ID {id} not found.");
		}

		question.QuestionCode = questionCode;
		question.Text = text;
		question.Type = type;
		question.IsRequired = isRequired;
		question.OptionsJson = optionsJson;
		question.HintText = hintText;
		question.ServiceCategoryId = serviceCategoryId;
		question.ParentQuestionId = parentQuestionId;
		question.DisplayOrder = displayOrder;
		question.VisibilityCondition = visibilityCondition;
		question.UpdatedAt = DateTime.UtcNow;

		var result = await questionService.UpdateQuestionAsync(question, cancellationToken);

		// Add/Update english text translation
		if (!string.IsNullOrWhiteSpace(englishText))
		{
			var resource = await context.LocalizationResources
				.FirstOrDefaultAsync(r => r.Key == text && r.Culture == "en", cancellationToken);
			if (resource != null)
			{
				resource.Value = englishText.Trim();
				resource.UpdatedAt = DateTime.UtcNow;
				context.LocalizationResources.Update(resource);
			}
			else
			{
				await context.LocalizationResources.AddAsync(new LocalizationResource
				{
					Id = Guid.NewGuid(),
					Key = text,
					Culture = "en",
					Value = englishText.Trim(),
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				}, cancellationToken);
			}
		}

		// Add/Update english hint translation
		if (!string.IsNullOrWhiteSpace(hintText) && !string.IsNullOrWhiteSpace(englishHint))
		{
			var resource = await context.LocalizationResources
				.FirstOrDefaultAsync(r => r.Key == hintText && r.Culture == "en", cancellationToken);
			if (resource != null)
			{
				resource.Value = englishHint.Trim();
				resource.UpdatedAt = DateTime.UtcNow;
				context.LocalizationResources.Update(resource);
			}
			else
			{
				await context.LocalizationResources.AddAsync(new LocalizationResource
				{
					Id = Guid.NewGuid(),
					Key = hintText,
					Culture = "en",
					Value = englishHint.Trim(),
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				}, cancellationToken);
			}
		}

		await context.SaveChangesAsync(cancellationToken);
		return result;
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<Question> UpdateQuestionLinks(
		Guid questionId,
		List<Guid> skuIds,
		List<Guid> formulaIds,
		[Service] IQuestionManagementService questionService,
		CancellationToken cancellationToken)
	{
		return await questionService.UpdateQuestionLinksAsync(questionId, skuIds, formulaIds, cancellationToken);
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<bool> DeleteQuestion(
		Guid questionId,
		[Service] IQuestionManagementService questionService,
		CancellationToken cancellationToken)
	{
		await questionService.DeleteQuestionAsync(questionId, cancellationToken);
		return true;
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<Formula> CreateFormula(
		string name,
		string description,
		string expression,
		[Service] IQuestionManagementService questionService,
		CancellationToken cancellationToken)
	{
		var formula = new Formula
		{
			Name = name,
			Description = description,
			Expression = expression,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		return await questionService.CreateFormulaAsync(formula, cancellationToken);
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<Formula> UpdateFormula(
		Guid id,
		string name,
		string description,
		string expression,
		[Service] IQuestionManagementService questionService,
		CancellationToken cancellationToken)
	{
		var formula = new Formula
		{
			Id = id,
			Name = name,
			Description = description,
			Expression = expression,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		return await questionService.UpdateFormulaAsync(formula, cancellationToken);
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<bool> DeleteFormula(
		Guid id,
		[Service] IQuestionManagementService questionService,
		CancellationToken cancellationToken)
	{
		await questionService.DeleteFormulaAsync(id, cancellationToken);
		return true;
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<BuildSmart.Core.Application.DTOs.OfferSimulationResultDto> RunOfferSimulation(
		List<Guid> selectedQuestionIds,
		string jobDetailsJson,
		[Service] IQuestionManagementService questionService,
		CancellationToken cancellationToken)
	{
		return await questionService.ExecuteOfferSimulationAsync(selectedQuestionIds, jobDetailsJson, cancellationToken);
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<BuildSmart.Api.DTOs.ImportResultDto> ImportSpiderNetConfig(
		string json,
		[Service] AppDbContext db)
	{
		var result = new BuildSmart.Api.DTOs.ImportResultDto();
		
		void LogInfo(string msg) => result.LogLines.Add($"[INFO] {DateTime.Now:HH:mm:ss} - {msg}");
		void LogSuccess(string msg) => result.LogLines.Add($"[SUCCESS] {DateTime.Now:HH:mm:ss} - {msg}");
		void LogWarning(string msg) => result.LogLines.Add($"[WARNING] {DateTime.Now:HH:mm:ss} - {msg}");
		void LogError(string msg) => result.LogLines.Add($"[ERROR] {DateTime.Now:HH:mm:ss} - {msg}");

		LogInfo("Starting transactional import...");

		using var transaction = await db.Database.BeginTransactionAsync();
		try
		{
			LogInfo("Parsing sync JSON configuration...");
			using var doc = System.Text.Json.JsonDocument.Parse(json);
			var root = doc.RootElement;

			LogInfo("Fetching active schema from database...");
			var liveCategories = await db.ServiceCategories.ToListAsync();
			var liveFormulas = await db.Formulas.ToListAsync();
			var liveQuestions = await db.Questions.ToListAsync();
			var liveSkus = await db.ServiceSkus.ToListAsync();
			LogInfo($"Loaded active schema: {liveCategories.Count} categories, {liveFormulas.Count} formulas, {liveQuestions.Count} questions.");

			// 0. Clear and remove all existing live Questions, Formulas, and SKUs (where possible)
			LogInfo("Analyzing database dependencies before cleanup...");
			foreach (var q in liveQuestions)
			{
				if (q.ParentQuestionId.HasValue)
				{
					q.ParentQuestionId = null;
					db.Questions.Update(q);
				}
			}
			await db.SaveChangesAsync();
			
			var failedToDeleteQuestions = new List<Question>();
			LogInfo("Attempting to delete old questions...");
			foreach (var q in liveQuestions)
			{
				try
				{
					db.Questions.Remove(q);
					await db.SaveChangesAsync();
					LogInfo($"Deleted old question: {q.QuestionCode}");
				}
				catch (Exception ex)
				{
					db.Entry(q).State = EntityState.Unchanged; // Reset state
					failedToDeleteQuestions.Add(q);
					LogWarning($"Failed to delete question '{q.QuestionCode}' (linked to existing projects). Retained in database: {ex.Message}");
				}
			}

			var failedToDeleteFormulas = new List<Formula>();
			LogInfo("Attempting to delete old formulas...");
			foreach (var f in liveFormulas)
			{
				try
				{
					db.Formulas.Remove(f);
					await db.SaveChangesAsync();
					LogInfo($"Deleted old formula: {f.Name}");
				}
				catch (Exception ex)
				{
					db.Entry(f).State = EntityState.Unchanged; // Reset state
					failedToDeleteFormulas.Add(f);
					LogWarning($"Failed to delete formula '{f.Name}' (referenced by other objects). Retained: {ex.Message}");
				}
			}

			LogInfo("Attempting to clean unused service SKUs...");
			var failedToDeleteSkus = new List<ServiceSku>();
			foreach (var s in liveSkus)
			{
				try
				{
					db.ServiceSkus.Remove(s);
					await db.SaveChangesAsync();
					LogInfo($"Deleted SKU: {s.SkuCode}");
				}
				catch (Exception ex)
				{
					db.Entry(s).State = EntityState.Unchanged; // Reset state
					failedToDeleteSkus.Add(s);
					LogInfo($"Retained active SKU in database: {s.SkuCode} ({ex.Message})");
				}
			}

			liveQuestions = failedToDeleteQuestions;
			liveFormulas = failedToDeleteFormulas;
			liveSkus = failedToDeleteSkus;

			var categoryMap = new Dictionary<Guid, Guid>();
			var formulaMap = new Dictionary<Guid, Guid>();
			var skuMap = new Dictionary<Guid, Guid>();
			var questionMap = new Dictionary<Guid, Guid>();

			var optionMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			if (root.TryGetProperty("LocalizationResources", out var lrArr))
			{
				foreach (var lrJson in lrArr.EnumerateArray())
				{
					var key = lrJson.GetProperty("Key").GetString() ?? "";
					var culture = lrJson.GetProperty("Culture").GetString() ?? "";
					var val = lrJson.GetProperty("Value").GetString() ?? "";
					if (culture == "en" && !string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(val))
					{
						optionMappings[key] = val;
					}
				}
			}

			// 1. Sync / Import Service Categories
			if (root.TryGetProperty("Categories", out var categoriesArr))
			{
				LogInfo($"Processing {categoriesArr.GetArrayLength()} categories from import block...");
				foreach (var cJson in categoriesArr.EnumerateArray())
				{
					var cName = cJson.GetProperty("Name").GetString() ?? "";
					var cDesc = cJson.TryGetProperty("Description", out var dProp) ? dProp.GetString() : null;
					var cIsGlobal = cJson.TryGetProperty("IsGlobal", out var igProp) && igProp.GetBoolean();
					var cTemplate = cJson.TryGetProperty("TemplateStructure", out var tProp) ? tProp.GetString() : "{}";
					var cStatusStr = cJson.TryGetProperty("Status", out var stProp) ? stProp.GetString() : "ACTIVE";
					Enum.TryParse<CategoryStatus>(cStatusStr, true, out var cStatus);
					var localId = cJson.GetProperty("Id").GetGuid();

					string? enName = null;
					if (cJson.TryGetProperty("Translations", out var ctArr))
					{
						foreach (var ctJson in ctArr.EnumerateArray())
						{
							var lang = ctJson.TryGetProperty("LanguageCode", out var lProp) ? lProp.GetString() : null;
							if (lang == "en")
							{
								enName = ctJson.TryGetProperty("Name", out var nProp) ? nProp.GetString() : null;
							}
						}
					}

					if (!string.IsNullOrWhiteSpace(cTemplate) && cTemplate != "{}")
					{
						try
						{
							var tsNode = System.Text.Json.Nodes.JsonNode.Parse(cTemplate);
							if (tsNode != null && tsNode["questions"] is System.Text.Json.Nodes.JsonArray qArray)
							{
								foreach (var qNode in qArray)
								{
									if (qNode is System.Text.Json.Nodes.JsonObject qObj)
									{
										// 1. Text translation enrichment
										var textNode = qObj["text"];
										if (textNode is System.Text.Json.Nodes.JsonValue valNode && valNode.TryGetValue<string>(out var textStr))
										{
											var enText = optionMappings.TryGetValue(textStr, out var et) ? et : textStr;
											qObj["text"] = new System.Text.Json.Nodes.JsonObject
											{
												["bg"] = textStr,
												["en"] = enText
											};
										}

										// 2. Options translation enrichment
										var optionsNode = qObj["options"];
										if (optionsNode is System.Text.Json.Nodes.JsonArray optArr)
										{
											var bgList = new System.Text.Json.Nodes.JsonArray();
											var enList = new System.Text.Json.Nodes.JsonArray();
											foreach (var optVal in optArr)
											{
												var optStr = optVal?.GetValue<string>() ?? "";
												bgList.Add(optStr);
												var enOpt = optionMappings.TryGetValue(optStr, out var eo) ? eo : optStr;
												enList.Add(enOpt);
											}
											qObj["options"] = new System.Text.Json.Nodes.JsonObject
											{
												["bg"] = bgList,
												["en"] = enList
											};
										}
									}
								}
								cTemplate = tsNode.ToJsonString();
							}
						}
						catch (Exception ex)
						{
							LogWarning($"Error enriching TemplateStructure: {ex.Message}");
						}
					}

					var existingCategory = liveCategories.FirstOrDefault(x => x.Id == localId || x.Name.Equals(cName, StringComparison.OrdinalIgnoreCase));
					if (existingCategory != null)
					{
						existingCategory.Name = cName;
						existingCategory.Description = cDesc;
						existingCategory.IsGlobal = cIsGlobal;
						existingCategory.TemplateStructure = cTemplate ?? "{}";
						existingCategory.Status = cStatus;
						existingCategory.EnglishName = enName?.Trim();
						existingCategory.EnglishDescription = cDesc;
						db.ServiceCategories.Update(existingCategory);
						await db.SaveChangesAsync();
						categoryMap[localId] = existingCategory.Id;
						LogSuccess($"Synced existing category: {cName}");
					}
					else
					{
						var created = new ServiceCategory
						{
							Id = Guid.NewGuid(),
							Name = cName,
							Description = cDesc,
							IsGlobal = cIsGlobal,
							TemplateStructure = cTemplate ?? "{}",
							Status = cStatus,
							EnglishName = enName?.Trim(),
							EnglishDescription = cDesc
						};
						db.ServiceCategories.Add(created);
						await db.SaveChangesAsync();
						categoryMap[localId] = created.Id;
						LogSuccess($"Created new category: {cName}");
					}
				}
			}

			// Deduplicate active global categories. If there are multiple active categories marked as IsGlobal, 
			// check if they share the same questions in their template. If so, deactivate the duplicate one.
			var activeGlobalCategories = await db.ServiceCategories
				.Where(c => c.IsGlobal && c.Status == CategoryStatus.Active)
				.ToListAsync();

			if (activeGlobalCategories.Count > 1)
			{
				LogWarning($"Found {activeGlobalCategories.Count} active global categories. Checking for duplicates...");
				var keptCategory = activeGlobalCategories.FirstOrDefault(c => c.Name.Equals("Global Questions", StringComparison.OrdinalIgnoreCase)) 
					?? activeGlobalCategories.First();

				foreach (var cat in activeGlobalCategories)
				{
					if (cat.Id != keptCategory.Id)
					{
						LogWarning($"Deactivating duplicate global category: '{cat.Name}' (ID: {cat.Id})");
						cat.IsGlobal = false;
						cat.Status = CategoryStatus.Draft;
						db.ServiceCategories.Update(cat);
					}
				}
				await db.SaveChangesAsync();
			}

			// 2. Sync / Import Formulas
			if (root.TryGetProperty("Formulas", out var formulasArr))
			{
				LogInfo($"Processing {formulasArr.GetArrayLength()} formulas from import block...");
				foreach (var fJson in formulasArr.EnumerateArray())
				{
					var fName = fJson.GetProperty("Name").GetString() ?? "";
					var fDesc = fJson.TryGetProperty("Description", out var fdProp) ? fdProp.GetString() ?? "" : "";
					var fExpr = fJson.GetProperty("Expression").GetString() ?? "";
					var localId = fJson.GetProperty("Id").GetGuid();

					var existingFormula = liveFormulas.FirstOrDefault(x => x.Id == localId || x.Name.Equals(fName, StringComparison.OrdinalIgnoreCase));
					if (existingFormula != null)
					{
						existingFormula.Name = fName;
						existingFormula.Description = fDesc;
						existingFormula.Expression = fExpr;
						db.Formulas.Update(existingFormula);
						await db.SaveChangesAsync();
						formulaMap[localId] = existingFormula.Id;
						LogSuccess($"Synced existing formula variable: {fName}");
					}
					else
					{
						var created = new Formula
						{
							Id = Guid.NewGuid(),
							Name = fName,
							Description = fDesc,
							Expression = fExpr
						};
						db.Formulas.Add(created);
						await db.SaveChangesAsync();
						formulaMap[localId] = created.Id;
						LogSuccess($"Created new formula variable: {fName}");
					}
				}
			}

			// 3. Sync / Import SKUs
			if (root.TryGetProperty("Skus", out var skusArr))
			{
				LogInfo($"Processing {skusArr.GetArrayLength()} SKUs from import block...");
				foreach (var sJson in skusArr.EnumerateArray())
				{
					var sCode = sJson.GetProperty("SkuCode").GetString() ?? "";
					var sName = sJson.GetProperty("Name").GetString() ?? "";
					var sDesc = sJson.TryGetProperty("Description", out var sdProp) ? sdProp.GetString() : null;
					var sPrice = sJson.GetProperty("BasePrice").GetDecimal();
					var sUnit = sJson.GetProperty("UnitType").GetString() ?? "";
					var sFormula = sJson.TryGetProperty("CalculationFormula", out var sfProp) ? sfProp.GetString() ?? "" : "";
					var sCatId = sJson.GetProperty("ServiceCategoryId").GetGuid();
					var localId = sJson.GetProperty("Id").GetGuid();
					
					Guid liveCatId = Guid.Empty;
					if (sCatId != Guid.Empty && categoryMap.TryGetValue(sCatId, out var mappedId))
					{
						liveCatId = mappedId;
					}
					else if (!string.IsNullOrEmpty(sName))
					{
						var matchedCat = liveCategories.FirstOrDefault(lc => lc.Name.Equals(sName, StringComparison.OrdinalIgnoreCase));
						if (matchedCat != null)
						{
							if (categoryMap.TryGetValue(matchedCat.Id, out var mappedCatId))
							{
								liveCatId = mappedCatId;
							}
							else
							{
								liveCatId = matchedCat.Id;
							}
						}
					}

					if (liveCatId == Guid.Empty)
					{
						LogError($"Skipping SKU {sCode} ({sName}): No matching service category could be resolved.");
						continue;
					}

					var enName = sJson.TryGetProperty("EnglishName", out var enNameProp) && enNameProp.ValueKind != JsonValueKind.Null ? enNameProp.GetString() : null;
					var enDesc = sJson.TryGetProperty("EnglishDescription", out var enDescProp) && enDescProp.ValueKind != JsonValueKind.Null ? enDescProp.GetString() : null;
					var enUnit = GetEnglishUnitType(sUnit);
					if (enUnit == sUnit && !string.IsNullOrWhiteSpace(sUnit) && optionMappings.TryGetValue(sUnit, out var mappedUnit))
					{
						enUnit = mappedUnit;
					}

					var existingSku = liveSkus.FirstOrDefault(x => x.SkuCode.Equals(sCode, StringComparison.OrdinalIgnoreCase));
					if (existingSku != null)
					{
						existingSku.Name = sName;
						existingSku.Description = sDesc;
						existingSku.BasePrice = sPrice;
						existingSku.UnitType = sUnit;
						existingSku.CalculationFormula = sFormula;
						existingSku.ServiceCategoryId = liveCatId;
						existingSku.EnglishName = enName?.Trim();
						existingSku.EnglishDescription = enDesc;
						existingSku.EnglishUnitType = enUnit;
						db.ServiceSkus.Update(existingSku);
						await db.SaveChangesAsync();
						skuMap[localId] = existingSku.Id;
						LogSuccess($"Synced SKU: {sCode} ({sName}) -> Formula: '{sFormula}', Unit: {sUnit}");
					}
					else
					{
						var created = new ServiceSku
						{
							Id = Guid.NewGuid(),
							SkuCode = sCode,
							Name = sName,
							Description = sDesc,
							BasePrice = sPrice,
							UnitType = sUnit,
							CalculationFormula = sFormula,
							ServiceCategoryId = liveCatId,
							EnglishName = enName?.Trim(),
							EnglishDescription = enDesc,
							EnglishUnitType = enUnit
						};
						db.ServiceSkus.Add(created);
						await db.SaveChangesAsync();
						skuMap[localId] = created.Id;
						LogSuccess($"Created SKU: {sCode} ({sName}) -> Formula: '{sFormula}', Unit: {sUnit}");
					}
				}
			}

			// 4. Import / Sync Questions (Pass 1 - Create/Update with ParentQuestionId = null)
			var importedQuestions = new List<(Question TempQuestion, Guid LocalId, List<Guid> SkuLinks, List<Guid> FormulaLinks)>();
			if (root.TryGetProperty("Questions", out var questionsArr))
			{
				LogInfo($"Processing {questionsArr.GetArrayLength()} questions (Pass 1 - creation)...");
				foreach (var qJson in questionsArr.EnumerateArray())
				{
					var qCode = qJson.GetProperty("QuestionCode").GetString() ?? "";
					var qText = qJson.GetProperty("Text").GetString() ?? "";
					var qType = qJson.GetProperty("Type").GetString() ?? "";
					var qReq = qJson.GetProperty("IsRequired").GetBoolean();
					var qOptions = qJson.TryGetProperty("OptionsJson", out var qoProp) ? qoProp.GetString() : null;
					var qHint = qJson.TryGetProperty("HintText", out var qhProp) ? qhProp.GetString() : null;
					var qCatId = qJson.TryGetProperty("ServiceCategoryId", out var qcProp) && qcProp.ValueKind != JsonValueKind.Null ? qcProp.GetGuid() : (Guid?)null;
					var qParentId = qJson.TryGetProperty("ParentQuestionId", out var qpProp) && qpProp.ValueKind != JsonValueKind.Null ? qpProp.GetGuid() : (Guid?)null;
					var qOrder = qJson.GetProperty("DisplayOrder").GetInt32();
					var qVis = qJson.TryGetProperty("VisibilityCondition", out var qvProp) ? qvProp.GetString() : null;
					
					var localId = qJson.GetProperty("Id").GetGuid();

					Guid? liveCatId = null;
					if (qCatId.HasValue)
					{
						if (categoryMap.TryGetValue(qCatId.Value, out var mappedId))
						{
							liveCatId = mappedId;
						}
					}

					var enText = qJson.TryGetProperty("EnglishText", out var etProp) && etProp.ValueKind != JsonValueKind.Null ? etProp.GetString() : null;
					var enHint = qJson.TryGetProperty("EnglishHint", out var ehProp) && ehProp.ValueKind != JsonValueKind.Null ? ehProp.GetString() : null;

					string? enOptionsJson = null;
					if (!string.IsNullOrEmpty(qOptions))
					{
						try
						{
							var bgOptions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(qOptions);
							if (bgOptions != null)
							{
								var enOptions = bgOptions.Select(opt => optionMappings.TryGetValue(opt, out var enOpt) ? enOpt : opt).ToList();
								enOptionsJson = System.Text.Json.JsonSerializer.Serialize(enOptions);
							}
						}
						catch { }
					}

					var existingQuestion = liveQuestions.FirstOrDefault(x => x.Id == localId || x.QuestionCode.Equals(qCode, StringComparison.OrdinalIgnoreCase));
					Question activeQ;
					if (existingQuestion != null)
					{
						existingQuestion.QuestionCode = qCode;
						existingQuestion.Text = qText;
						existingQuestion.Type = qType;
						existingQuestion.IsRequired = qReq;
						existingQuestion.OptionsJson = qOptions;
						existingQuestion.HintText = qHint;
						existingQuestion.ServiceCategoryId = liveCatId;
						existingQuestion.ParentQuestionId = null; // null for Pass 1
						existingQuestion.DisplayOrder = qOrder;
						existingQuestion.VisibilityCondition = qVis;
						existingQuestion.EnglishText = enText?.Trim();
						existingQuestion.EnglishHint = enHint?.Trim();
						existingQuestion.EnglishOptionsJson = enOptionsJson;
						db.Questions.Update(existingQuestion);
						await db.SaveChangesAsync();
						questionMap[localId] = existingQuestion.Id;
						activeQ = existingQuestion;
						LogSuccess($"Synced question: {qCode}");
					}
					else
					{
						var created = new Question
						{
							Id = Guid.NewGuid(),
							QuestionCode = qCode,
							Text = qText,
							Type = qType,
							IsRequired = qReq,
							OptionsJson = qOptions,
							HintText = qHint,
							ServiceCategoryId = liveCatId,
							ParentQuestionId = null, // null for Pass 1
							DisplayOrder = qOrder,
							VisibilityCondition = qVis,
							EnglishText = enText?.Trim(),
							EnglishHint = enHint?.Trim(),
							EnglishOptionsJson = enOptionsJson
						};
						db.Questions.Add(created);
						await db.SaveChangesAsync();
						questionMap[localId] = created.Id;
						activeQ = created;
						LogSuccess($"Created question: {qCode}");
					}

					// Fetch SKU and Formula links from JSON
					var skuLinks = new List<Guid>();
					if (qJson.TryGetProperty("SkuIds", out var skusLinkArr))
					{
						foreach (var item in skusLinkArr.EnumerateArray())
						{
							skuLinks.Add(item.GetGuid());
						}
					}
					var formulaLinks = new List<Guid>();
					if (qJson.TryGetProperty("FormulaIds", out var formulasLinkArr))
					{
						foreach (var item in formulasLinkArr.EnumerateArray())
						{
							formulaLinks.Add(item.GetGuid());
						}
					}

					importedQuestions.Add((activeQ, localId, skuLinks, formulaLinks));
				}
			}

			// 5. Update Links
			LogInfo("Updating question links...");
			foreach (var item in importedQuestions)
			{
				var mappedSkuIds = item.SkuLinks
					.Select(localSkuId => skuMap.TryGetValue(localSkuId, out var liveSkuId) ? liveSkuId : (Guid?)null)
					.Where(id => id.HasValue)
					.Select(id => id!.Value)
					.ToList();

				var mappedFormulaIds = item.FormulaLinks
					.Select(localFormulaId => formulaMap.TryGetValue(localFormulaId, out var liveFormulaId) ? liveFormulaId : (Guid?)null)
					.Where(id => id.HasValue)
					.Select(id => id!.Value)
					.ToList();

				var q = await db.Questions
					.Include(x => x.Skus)
					.Include(x => x.Formulas)
					.FirstAsync(x => x.Id == item.TempQuestion.Id);

				q.SkuIds = mappedSkuIds;
				q.Skus.Clear();
				foreach (var skuId in mappedSkuIds)
				{
					var sku = await db.ServiceSkus.FindAsync(skuId);
					if (sku != null) q.Skus.Add(sku);
				}

				q.FormulaIds = mappedFormulaIds;
				q.Formulas.Clear();
				foreach (var fId in mappedFormulaIds)
				{
					var f = await db.Formulas.FindAsync(fId);
					if (f != null) q.Formulas.Add(f);
				}

				db.Questions.Update(q);
				await db.SaveChangesAsync();
				if (mappedSkuIds.Any() || mappedFormulaIds.Any())
				{
					LogInfo($"  Linked {item.TempQuestion.QuestionCode} -> SKUs: {mappedSkuIds.Count}, Formulas: {mappedFormulaIds.Count}");
				}
			}

			// 6. Link Parents (Pass 2)
			LogInfo("Linking question dependencies (Pass 2)...");
			foreach (var item in importedQuestions)
			{
				var parentIdProp = root.GetProperty("Questions")
					.EnumerateArray()
					.First(x => x.GetProperty("Id").GetGuid() == item.LocalId)
					.TryGetProperty("ParentQuestionId", out var pProp) && pProp.ValueKind != JsonValueKind.Null ? pProp.GetGuid() : (Guid?)null;

				if (parentIdProp.HasValue)
				{
					if (questionMap.TryGetValue(parentIdProp.Value, out var liveParentId))
					{
						item.TempQuestion.ParentQuestionId = liveParentId;
						db.Questions.Update(item.TempQuestion);
						await db.SaveChangesAsync();
						LogInfo($"  Dependency set: {item.TempQuestion.QuestionCode} depends on parent.");
					}
				}
			}

			// 7. Sync Localization Resources (like option translations)
			if (root.TryGetProperty("LocalizationResources", out var locResArr))
			{
				LogInfo($"Processing {locResArr.GetArrayLength()} extra localization resources from import block...");
				foreach (var locJson in locResArr.EnumerateArray())
				{
					var key = locJson.GetProperty("Key").GetString() ?? "";
					var culture = locJson.GetProperty("Culture").GetString() ?? "";
					var val = locJson.GetProperty("Value").GetString() ?? "";

					if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(culture))
					{
						var existingLoc = await db.LocalizationResources
							.FirstOrDefaultAsync(x => x.Key == key && x.Culture == culture);
						if (existingLoc != null)
						{
							existingLoc.Value = val.Trim();
							existingLoc.UpdatedAt = DateTime.UtcNow;
							db.LocalizationResources.Update(existingLoc);
						}
						else
						{
							await db.LocalizationResources.AddAsync(new LocalizationResource
							{
								Id = Guid.NewGuid(),
								Key = key,
								Culture = culture,
								Value = val.Trim(),
								CreatedAt = DateTime.UtcNow,
								UpdatedAt = DateTime.UtcNow
							});
						}

						if (culture == "en")
						{
							var existingBg = await db.LocalizationResources
								.FirstOrDefaultAsync(x => x.Key == key && x.Culture == "bg");
							if (existingBg != null)
							{
								existingBg.Value = key.Trim();
								existingBg.UpdatedAt = DateTime.UtcNow;
								db.LocalizationResources.Update(existingBg);
							}
							else
							{
								await db.LocalizationResources.AddAsync(new LocalizationResource
								{
									Id = Guid.NewGuid(),
									Key = key,
									Culture = "bg",
									Value = key.Trim(),
									CreatedAt = DateTime.UtcNow,
									UpdatedAt = DateTime.UtcNow
								});
							}
						}
					}
				}
				await db.SaveChangesAsync();
			}

			// Unlink any old questions that failed to delete and are not in the new config
			var importedQuestionIds = questionMap.Values.ToList();
			var orphanedQuestions = failedToDeleteQuestions.Where(q => !importedQuestionIds.Contains(q.Id)).ToList();
			if (orphanedQuestions.Any())
			{
				LogInfo($"Unlinking {orphanedQuestions.Count} orphaned old questions from their categories...");
				foreach (var oq in orphanedQuestions)
				{
					oq.ServiceCategoryId = null;
					oq.ParentQuestionId = null;
					db.Questions.Update(oq);
				}
				await db.SaveChangesAsync();
			}

			// Unlink any old SKUs that failed to delete and are not in the new config
			var importedSkuIds = skuMap.Values.ToList();
			var orphanedSkus = failedToDeleteSkus.Where(s => !importedSkuIds.Contains(s.Id)).ToList();
			if (orphanedSkus.Any())
			{
				LogInfo($"Unlinking {orphanedSkus.Count} orphaned old SKUs from their categories...");
				foreach (var os in orphanedSkus)
				{
					os.ServiceCategoryId = Guid.Empty;
					db.ServiceSkus.Update(os);
				}
				await db.SaveChangesAsync();
			}

			LogSuccess("All steps completed successfully. Committing transaction...");
			await transaction.CommitAsync();
			result.Success = true;
			result.ErrorMessage = null;
		}
		catch (Exception ex)
		{
			LogError($"FATAL ERROR: {ex.Message}");
			LogWarning("Rolling back transaction! No database changes have been applied.");
			await transaction.RollbackAsync();
			result.Success = false;
			result.ErrorMessage = ex.Message;
		}

		return result;
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<LocalizationResource> UpdateLocalizationString(
		UpdateLocalizationInput input,
		ClaimsPrincipal claimsPrincipal,
		[Service] AppDbContext context,
		[Service] IHubContext<NotificationHub> hubContext)
	{
		var resource = await context.LocalizationResources
			.FirstOrDefaultAsync(r => r.Key == input.Key && r.Culture == input.Culture);

		var username = claimsPrincipal.Identity?.Name ?? "Admin";

		if (resource == null)
		{
			resource = new LocalizationResource
			{
				Id = Guid.NewGuid(),
				Key = input.Key,
				Culture = input.Culture,
				Value = input.Value,
				UpdatedBy = username,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};
			await context.LocalizationResources.AddAsync(resource);
		}
		else
		{
			resource.Value = input.Value;
			resource.UpdatedBy = username;
			resource.UpdatedAt = DateTime.UtcNow;
			context.LocalizationResources.Update(resource);
		}

		await context.SaveChangesAsync();

		// Broadcast update to all SignalR clients
		await hubContext.Clients.All.SendAsync("ReceiveLocalizationUpdate", input.Key, input.Culture, input.Value);

		return resource;
	}

	private static string GetEnglishUnitType(string unitType)
	{
		if (string.IsNullOrWhiteSpace(unitType)) return string.Empty;
		var trimmed = unitType.Trim().ToLower();
		if (trimmed == "бр." || trimmed == "бр") return "pc";
		if (trimmed == "кв.м." || trimmed == "кв. м." || trimmed == "кв.м") return "sq.m.";
		if (trimmed == "л.м." || trimmed == "лин.м" || trimmed == "лин. м." || trimmed == "л. м.") return "lm";
		if (trimmed == "модул") return "module";
		return unitType;
	}
}

public class UpdateLocalizationInput
{
	public string Key { get; set; } = null!;
	public string Culture { get; set; } = null!;
	public string Value { get; set; } = null!;
}

public class AnonymousChatPayload
{
	public string Token { get; set; } = null!;
	public Guid ProjectId { get; set; }
}