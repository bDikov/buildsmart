using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class AiCalculationTask : BaseEntity
{
    public Guid AiCalculationId { get; set; }
    public AiCalculation AiCalculation { get; set; } = null!;

    public string Title { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public decimal EstimatedPrice { get; set; }

    public ICollection<AiCalculationSkuItem> SkuItems { get; set; } = new List<AiCalculationSkuItem>();
    public ICollection<AiCalculationCriteria> AcceptanceCriteria { get; set; } = new List<AiCalculationCriteria>();
}