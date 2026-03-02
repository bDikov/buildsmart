using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Application.Services;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Core.Domain.ValueObjects;
using HotChocolate.Authorization;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
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
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
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
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
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
			videoUrl = await storageService.SaveFileAsync(stream, file.Name, file.ContentType);
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

	public async Task<User> RegisterUser(
		string firstName,
		string lastName,
		string email,
		string password,
		[Service] IAuthService authService)
	{
		return await authService.RegisterUserAsync(firstName, lastName, email, password);
	}

	[Authorize]
	public async Task<User> UpdateUserProfile(
		Guid userId,
		string firstName,
		string lastName,
		string? bio,
		string? location,
		string? profilePictureUrl,
		[Service] IAuthService authService)
	{
		return await authService.UpdateUserProfileAsync(userId, firstName, lastName, bio, location, profilePictureUrl);
	}

	public async Task<Booking> CreateBooking(
		Guid homeownerId,
		Guid tradesmanProfileId,
		DateTime requestedDate,
		string jobDescription,
		[Service] IBookingService bookingService)
	{
		return await bookingService.CreateBookingAsync(
			homeownerId,
			tradesmanProfileId,
			requestedDate,
			jobDescription
		);
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
		[Service] IJobPostService jobPostService)
	{
		return await jobPostService.CreateProjectAsync(homeownerId, title, description);
	}

	[Authorize]
	public async Task<bool> DeleteProject(
		Guid projectId,
		ClaimsPrincipal claimsPrincipal,
		[Service] IUnitOfWork unitOfWork)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
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

		await unitOfWork.Projects.DeleteAsync(projectId);
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
			imageUrls
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

	public async Task<bool> SubmitJobPost(
		Guid jobPostId,
		[Service] IJobPostService jobPostService)
	{
		await jobPostService.SubmitJobPostAsync(jobPostId);
		return true;
	}

	public async Task<Bid> SubmitBid(
		Guid tradesmanProfileId,
		Guid jobPostId,
		decimal subtotal,
		string currency,
		string? comment,
		[Service] IJobPostService jobPostService)
	{
		var amount = Amount.Create(currency, subtotal);
		return await jobPostService.SubmitBidAsync(tradesmanProfileId, jobPostId, amount, comment);
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

    [Authorize(Roles = new[] { "Tradesman" })]
    public async Task<JobPostQuestion> EditJobQuestion(
        Guid questionId,
        string newText,
        ClaimsPrincipal claimsPrincipal,
        [Service] IUnitOfWork unitOfWork,
        [Service] IJobPostService jobPostService)
    {
        var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new GraphQLException("Invalid user credentials.");
        }

        var profile = await unitOfWork.TradesmanProfiles.GetByUserIdAsync(userId);
        if (profile == null)
        {
            throw new GraphQLException("Tradesman profile not found.");
        }

        return await jobPostService.EditJobQuestionAsync(questionId, profile.Id, newText);
    }

    [Authorize(Roles = new[] { "Homeowner" })]
    public async Task<JobPostQuestion> EditJobAnswer(
        Guid questionId,
        string newAnswer,
        ClaimsPrincipal claimsPrincipal,
        [Service] IUnitOfWork unitOfWork,
        [Service] IJobPostService jobPostService)
    {
        var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
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
        var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new GraphQLException("Invalid user credentials.");
        }

        return await jobPostService.ReplyToQuestionAsync(parentQuestionId, userId, replyText);
    }

	public async Task<Booking> AcceptBid(
		Guid bidId,
		[Service] IJobPostService jobPostService)
	{
		return await jobPostService.AcceptBidAsync(bidId);
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

	[Authorize]
	public async Task<JobPostFeedback> AddJobFeedback(
		Guid jobPostId,
		string text,
		ClaimsPrincipal claimsPrincipal,
		[Service] IJobPostService jobPostService)
	{
		var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
		if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
		{
			throw new GraphQLException("Invalid user credentials.");
		}

		return await jobPostService.AddFeedbackAsync(jobPostId, userId, text);
	}

	[Authorize(Roles = new[] { "Admin" })]
	public async Task<bool> ResolveJobFeedback(
		Guid feedbackId,
		[Service] IJobPostService jobPostService)
	{
		await jobPostService.ResolveFeedbackAsync(feedbackId);
		return true;
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
		[Service] IUnitOfWork unitOfWork)
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
				Name = name,
				Description = description,
				IsGlobal = isGlobal,
				TemplateStructure = templateStructure,
				Status = status ?? CategoryStatus.Draft
			};
			await unitOfWork.ServiceCategories.AddAsync(category);
		}
				await unitOfWork.SaveChangesAsync();
				return category;
			}
		
		    [Authorize]
		    public async Task<bool> DeleteAllNotifications(
		        ClaimsPrincipal claimsPrincipal,
		        [Service] IUnitOfWork unitOfWork)
		    {
		        var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
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
		}
		