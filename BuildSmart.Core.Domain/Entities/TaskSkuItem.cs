using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class TaskSkuItem : BaseEntity
{
    public Guid JobTaskId { get; set; }
    public JobTask JobTask { get; set; } = null!;
    
    public Guid ServiceSkuId { get; set; }
    public ServiceSku ServiceSku { get; set; } = null!;
    
    public decimal Quantity { get; set; } = 1;
    public decimal EstimatedPrice { get; set; } // Quantity * BasePrice
}
