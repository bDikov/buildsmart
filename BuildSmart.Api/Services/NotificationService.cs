using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BuildSmart.Api.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IServiceProvider _serviceProvider;

    public NotificationService(IHubContext<NotificationHub> hubContext, IServiceProvider serviceProvider)
    {
        _hubContext = hubContext;
        _serviceProvider = serviceProvider;
    }

    public async Task SendNotificationAsync(Guid userId, string title, string message, Guid? relatedEntityId = null, string? relatedEntityType = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // 1. Persist to Database
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await unitOfWork.Notifications.AddAsync(notification);
        await unitOfWork.SaveChangesAsync();

        // 2. Send via SignalR (Target specific user by ID)
        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", title, message);
    }
}
