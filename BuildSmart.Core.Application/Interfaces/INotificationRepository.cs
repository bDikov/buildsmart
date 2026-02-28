using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
    Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(Guid userId);
    Task<IEnumerable<Notification>> GetAllByUserIdAsync(Guid userId);
    Task MarkAsReadAsync(Guid notificationId);
    Task DeleteAllByUserIdAsync(Guid userId);
}
