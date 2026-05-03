using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class AiCalculation : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid ServiceCategoryId { get; set; }
    public decimal TotalEstimatedPrice { get; set; }
    
    public ICollection<AiCalculationTask> Tasks { get; set; } = new List<AiCalculationTask>();
}