using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IAuthService
{
    Task<User> RegisterUserAsync(string firstName, string lastName, string email, string password);
    Task<User> UpdateUserProfileAsync(Guid userId, string firstName, string lastName, string? bio, string? location, string? profilePictureUrl);
    Task<User> UpdateUserRoleAndCategoriesAsync(Guid userId, BuildSmart.Core.Domain.Enums.UserRoleTypes newRole, List<Guid>? serviceCategoryIds);
    Task<bool> VerifyEmailAsync(string token);
    Task<string> GenerateJwtTokenForExternalLogin(string email, string name);
}
