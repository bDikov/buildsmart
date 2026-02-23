namespace BuildSmart.Core.Application.Interfaces;

public interface INotificationService
{
    Task SendNotificationAsync(Guid userId, string title, string message, Guid? relatedEntityId = null, string? relatedEntityType = null);
}
