using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; } = false;
    
    // Optional: Link to a specific entity (e.g., JobPost ID)
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; } // "JobPost", "Booking", etc.
}
