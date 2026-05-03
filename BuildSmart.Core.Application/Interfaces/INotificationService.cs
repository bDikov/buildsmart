namespace BuildSmart.Core.Application.Interfaces;

public interface INotificationService
{
    Task SendNotificationAsync(Guid userId, string title, string message, Guid? relatedEntityId = null, string? relatedEntityType = null, object? data = null);
    Task SendLocalizedNotificationAsync(Guid userId, string titleKey, string messageKey, object[]? messageArgs = null, Guid? relatedEntityId = null, string? relatedEntityType = null, object? data = null);
    Task NotifyAuctionGroupAsync(Guid jobPostId, string method, object payload);
}
