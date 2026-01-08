using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;

namespace BuildSmart.Core.Application.Interfaces;

/// <summary>
/// Interface for tradesman profile management services.
/// </summary>
public interface ITradesmanProfileService
{
	Task<TradesmanProfile> UpdateProfileAsync(Guid tradesmanProfileId, string bio, string location, List<Guid> serviceCategoryIds);
	Task<PortfolioEntry> AddPortfolioEntryAsync(Guid tradesmanProfileId, string title, string description, string imageUrl, string? videoUrl);
    
    // Removed VerifyTradesmanAsync as we now do per-skill verification
    Task UpdateSkillVerificationAsync(Guid tradesmanProfileId, Guid categoryId, SkillVerificationStatus status);
    Task CheckCommunityVerificationAsync(Guid tradesmanProfileId, Guid categoryId);
}