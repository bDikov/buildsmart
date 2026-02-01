using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Application.Services;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Core.Domain.ValueObjects;
using HotChocolate.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BuildSmart.Api.GraphQL;

public class Mutation
{
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

	public async Task<Booking> AcceptBid(
		Guid bidId,
		[Service] IJobPostService jobPostService)
	{
		return await jobPostService.AcceptBidAsync(bidId);
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
}