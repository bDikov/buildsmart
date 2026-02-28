using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace BuildSmart.Api.Hubs;

public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        // Use the NameIdentifier claim (which we store the User ID in)
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
