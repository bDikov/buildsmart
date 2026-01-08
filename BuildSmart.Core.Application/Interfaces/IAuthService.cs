using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IAuthService
{
    Task<User> RegisterUserAsync(string firstName, string lastName, string email, string password);
    Task<bool> VerifyEmailAsync(string token);
    Task<string> GenerateJwtTokenForExternalLogin(string email, string name);
}
