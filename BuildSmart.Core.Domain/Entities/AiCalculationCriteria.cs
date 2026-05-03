using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class AiCalculationCriteria : BaseEntity
{
    public Guid AiCalculationTaskId { get; set; }
    public AiCalculationTask AiCalculationTask { get; set; } = null!;

    public string Description { get; set; } = null!;
}