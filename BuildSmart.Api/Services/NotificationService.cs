using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Localization;
using BuildSmart.Core.Application.Resources;
using System.Globalization;

namespace BuildSmart.Api.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly IStringLocalizer<NotificationResources> _localizer;

    public NotificationService(IHubContext<NotificationHub> hubContext, IServiceProvider serviceProvider, IStringLocalizer<NotificationResources> localizer)
    {
        _hubContext = hubContext;
        _serviceProvider = serviceProvider;
        _localizer = localizer;
    }

    public async Task SendNotificationAsync(Guid userId, string title, string message, Guid? relatedEntityId = null, string? relatedEntityType = null, object? data = null)
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
        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", title, message, data);
    }

    public async Task SendLocalizedNotificationAsync(Guid userId, string titleKey, string messageKey, object[]? messageArgs = null, Guid? relatedEntityId = null, string? relatedEntityType = null, object? data = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Fetch the user to get their preferred language
        var targetUser = await unitOfWork.Users.GetByIdAsync(userId);
        var targetLang = targetUser?.PreferredLanguage ?? "en";

        // Store original culture
        var originalCulture = CultureInfo.CurrentUICulture;

        string title;
        string message;

        try
        {
            // Switch culture for localization
            CultureInfo.CurrentUICulture = new CultureInfo(targetLang);

            title = _localizer[titleKey];
            var rawMessage = _localizer[messageKey];
            
            // Apply formatting if arguments are provided
            message = messageArgs != null && messageArgs.Length > 0 
                ? string.Format(rawMessage, messageArgs) 
                : rawMessage;
        }
        finally
        {
            // Restore original culture to prevent side-effects on the current request/worker thread
            CultureInfo.CurrentUICulture = originalCulture;
        }

        // Persist and send the localized version
        await SendNotificationAsync(userId, title, message, relatedEntityId, relatedEntityType, data);
    }

    public async Task NotifyAuctionGroupAsync(Guid jobPostId, string method, object payload)
    {
        await _hubContext.Clients.Group($"Auction_{jobPostId}").SendAsync(method, payload);
    }
}
