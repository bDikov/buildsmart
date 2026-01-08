using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class HomeownerProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // Address for billing or default service location
    public string? Address { get; set; }
    
    // In the future: SavedPaymentMethods, etc.
}
