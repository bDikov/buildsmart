using Microsoft.AspNetCore.SignalR;

namespace BuildSmart.Api.Hubs;

public class JobProcessingHub : Hub
{
    // Clients don't need to send messages to the hub, they just listen.
    // However, if we need connection management, we can add it here.
    
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public async Task JoinProjectGroup(string projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, projectId);
    }

    public async Task LeaveProjectGroup(string projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, projectId);
    }
}
