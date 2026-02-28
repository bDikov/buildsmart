using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace BuildSmart.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    // Groups are handled automatically by Clients.User() when IUserIdProvider is registered
}
