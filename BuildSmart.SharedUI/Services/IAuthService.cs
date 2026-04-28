using System.Threading.Tasks;
using System;
namespace BuildSmart.SharedUI.Services;

public interface IAuthService
{
    Task<string?> GetTokenAsync();
    Task SaveTokenAsync(string token);
    Task ClearTokenAsync();
    bool IsAuthenticated { get; }
    string? GetUserRoleFromToken(string? token);
    Guid? GetUserIdFromToken(string? token);
}
