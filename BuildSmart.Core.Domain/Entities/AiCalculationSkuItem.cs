using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class AiCalculationSkuItem : BaseEntity
{
    public Guid AiCalculationTaskId { get; set; }
    public AiCalculationTask AiCalculationTask { get; set; } = null!;

    public Guid ServiceSkuId { get; set; }
    public ServiceSku ServiceSku { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal EstimatedPrice { get; set; }
}