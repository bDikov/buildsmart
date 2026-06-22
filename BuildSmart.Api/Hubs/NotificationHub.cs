using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using BuildSmart.Core.Application.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BuildSmart.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly IActiveProjectChatTracker _tracker;
    private readonly IUserPresenceService _presenceService;

    public NotificationHub(IActiveProjectChatTracker tracker, IUserPresenceService presenceService)
    {
        _tracker = tracker;
        _presenceService = presenceService;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            var wasOnline = _presenceService.IsUserOnline(userId);
            _presenceService.UserConnected(Context.ConnectionId, userId);
            if (!wasOnline)
            {
                await Clients.All.SendAsync("UserPresenceChanged", userId, true);
            }
        }
    }

    // Groups are handled automatically by Clients.User() when IUserIdProvider is registered
    
    public async Task JoinAuctionGroup(string jobPostId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Auction_{jobPostId}");
    }

    public async Task LeaveAuctionGroup(string jobPostId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Auction_{jobPostId}");
    }

    public async Task JoinProjectGroup(string projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Project_{projectId}");
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            _tracker.JoinProject(Context.ConnectionId, userId, projectId);
        }
    }

    public async Task LeaveProjectGroup(string projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Project_{projectId}");
        _tracker.LeaveProject(Context.ConnectionId, projectId);
    }

    public async Task JoinSupportGroup()
    {
        var isAdmin = Context.User?.Claims.Any(c => 
            (c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role") && 
            string.Equals(c.Value, "admin", StringComparison.OrdinalIgnoreCase)) ?? false;

        if (isAdmin)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Support");
        }
    }

    public async Task LeaveSupportGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Support");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _tracker.RemoveConnection(Context.ConnectionId);
        var userId = _presenceService.UserDisconnected(Context.ConnectionId);
        if (!string.IsNullOrEmpty(userId))
        {
            var isStillOnline = _presenceService.IsUserOnline(userId);
            if (!isStillOnline)
            {
                await Clients.All.SendAsync("UserPresenceChanged", userId, false);
            }
        }
        await base.OnDisconnectedAsync(exception);
    }
}
