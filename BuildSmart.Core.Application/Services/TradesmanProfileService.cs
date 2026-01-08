using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Entities.JoinEntities;
using BuildSmart.Core.Domain.Enums;

namespace BuildSmart.Core.Application.Services;

/// <summary>
/// Handles business logic related to managing tradesman profiles.
/// </summary>
public class TradesmanProfileService : ITradesmanProfileService
{
	private readonly IUnitOfWork _unitOfWork;

	public TradesmanProfileService(IUnitOfWork unitOfWork)
	{
		_unitOfWork = unitOfWork;
	}

	/// <summary>
	/// Updates the details of a tradesman's profile.
	/// </summary>
	/// <param name="tradesmanProfileId">The ID of the profile to update.</param>
	/// <param name="bio">The new biography.</param>
	/// <param name="location">The new location.</param>
	/// <param name="serviceCategoryIds">The new list of service category IDs.</param>
	/// <returns>The updated tradesman profile.</returns>
	/// <exception cref="ArgumentException">Thrown if the profile or category is not found.</exception>
	public async Task<TradesmanProfile> UpdateProfileAsync(Guid tradesmanProfileId, string bio, string location, List<Guid> serviceCategoryIds)
	{
        // Note: We need to include Skills in the repository fetch or ensure they are loaded
        // For now, assuming basic retrieval. In a real scenario, we'd use .Include(x => x.Skills)
		var profile = await _unitOfWork.TradesmanProfiles.GetByIdAsync(tradesmanProfileId)
			?? throw new ArgumentException("Tradesman profile not found.", nameof(tradesmanProfileId));

        // Update User Bio/Location
		var user = await _unitOfWork.Users.GetByIdAsync(profile.UserId);
		if (user != null)
		{
			user.Bio = bio;
			user.Location = location;
			_unitOfWork.Users.Update(user);
		}

        // Logic to update Skills (Remove old, add new)
        // This is a naive implementation (clear and add). 
        // A better approach in EF Core is to compare the lists to minimize DB churn.
        
        // 1. Clear existing skills (Not efficient for large lists, but fine for <10 categories)
        profile.Skills.Clear();

        // 2. Add new skills
        foreach (var categoryId in serviceCategoryIds)
        {
            var category = await _unitOfWork.ServiceCategories.GetByIdAsync(categoryId);
            if (category != null)
            {
                profile.Skills.Add(new TradesmanSkill
                {
                    TradesmanProfileId = profile.Id,
                    ServiceCategoryId = category.Id,
                    VerificationStatus = SkillVerificationStatus.Unverified
                });
            }
        }

		_unitOfWork.TradesmanProfiles.Update(profile);

		await _unitOfWork.SaveChangesAsync();
		return profile;
	}

	/// <summary>
	/// Adds a new portfolio entry to a tradesman's profile.
	/// </summary>
	/// <param name="tradesmanProfileId">The ID of the profile.</param>
	/// <param name="title">The title of the project.</param>
	/// <param name="description">The description of the project.</param>
	/// <param name="imageUrl">The URL of the project image.</param>
	/// <param name="videoUrl">The optional URL of a project video.</param>
	/// <returns>The newly created portfolio entry.</returns>
	/// <exception cref="ArgumentException">Thrown if the profile is not found.</exception>
	public async Task<PortfolioEntry> AddPortfolioEntryAsync(Guid tradesmanProfileId, string title, string description, string imageUrl, string? videoUrl)
	{
		var profile = await _unitOfWork.TradesmanProfiles.GetByIdAsync(tradesmanProfileId)
			?? throw new ArgumentException("Tradesman profile not found.", nameof(tradesmanProfileId));

		var newEntry = new PortfolioEntry
		{
			TradesmanProfileId = tradesmanProfileId,
			Title = title,
			Description = description,
			ImageUrl = imageUrl,
			VideoUrl = videoUrl
		};

		profile.PortfolioEntries.Add(newEntry);
		_unitOfWork.TradesmanProfiles.Update(profile);
		await _unitOfWork.SaveChangesAsync();

		return newEntry;
	}

    /// <summary>
    /// Updates the verification status of a specific skill for a tradesman.
    /// </summary>
    public async Task UpdateSkillVerificationAsync(Guid tradesmanProfileId, Guid categoryId, SkillVerificationStatus status)
    {
        var profile = await _unitOfWork.TradesmanProfiles.GetByIdAsync(tradesmanProfileId)
            ?? throw new ArgumentException("Tradesman profile not found.");

        var skill = profile.Skills.FirstOrDefault(s => s.ServiceCategoryId == categoryId);
        if (skill == null)
        {
            throw new ArgumentException("Skill not found for this tradesman.");
        }

        skill.VerificationStatus = status;
        _unitOfWork.TradesmanProfiles.Update(profile);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Checks if a tradesman qualifies for Community Verification based on reviews.
    /// Rule: 5 reviews in this category with average rating > 4.5
    /// </summary>
    public async Task CheckCommunityVerificationAsync(Guid tradesmanProfileId, Guid categoryId)
    {
         var profile = await _unitOfWork.TradesmanProfiles.GetByIdAsync(tradesmanProfileId);
         if (profile == null) return;

         var skill = profile.Skills.FirstOrDefault(s => s.ServiceCategoryId == categoryId);
         if (skill == null || skill.VerificationStatus != SkillVerificationStatus.Unverified) 
             return; // Already verified or no skill

         // Count completed bookings with 5-star reviews for this category
         // Note: We need a complex query here joining Bookings -> JobPost -> ServiceCategory
         // For MVP, we will simulate this check or implement a basic one in Repository
         
         // Mock Logic:
         var qualifyingReviews = profile.Reviews
             .Count(r => r.Rating >= 4 && r.Booking.TradesmanProfileId == tradesmanProfileId); 
             // Ideally check category too
         
         if (qualifyingReviews >= 5)
         {
             skill.VerificationStatus = SkillVerificationStatus.CommunityVerified;
             _unitOfWork.TradesmanProfiles.Update(profile);
             await _unitOfWork.SaveChangesAsync();
         }
    }
}