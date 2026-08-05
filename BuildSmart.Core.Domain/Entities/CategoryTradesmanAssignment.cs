using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class CategoryTradesmanAssignment : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid JobPostId { get; set; }
    public JobPost JobPost { get; set; } = null!;

    public Guid ServiceCategoryId { get; set; }
    public ServiceCategory ServiceCategory { get; set; } = null!;

    public Guid TradesmanId { get; set; }
    public User Tradesman { get; set; } = null!;

    public Guid AssignedByAdminId { get; set; }
    public User AssignedByAdmin { get; set; } = null!;
}
