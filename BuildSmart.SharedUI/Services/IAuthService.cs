using System.Threading.Tasks;
using System;
namespace BuildSmart.SharedUI.Services;

public interface IAuthService
{
    Task<string?> GetTokenAsync();
    Task SaveTokenAsync(string token);
    Task ClearTokenAsync();
    Task<string?> RenewTokenAsync(string? currentToken = null);
    bool IsAuthenticated { get; }
    string? GetUserRoleFromToken(string? token);
    Guid? GetUserIdFromToken(string? token);
    Task<string?> AuthenticateWithGoogleAsync();
    Task<string?> AuthenticateWithFacebookAsync();
    Task<string?> AuthenticateWithAppleAsync();
}
