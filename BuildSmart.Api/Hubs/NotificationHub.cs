using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace BuildSmart.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    // Groups are handled automatically by Clients.User() when IUserIdProvider is registered
    
    public async Task JoinAuctionGroup(string jobPostId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Auction_{jobPostId}");
    }

    public async Task LeaveAuctionGroup(string jobPostId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Auction_{jobPostId}");
    }
}
