using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class ServiceSku : BaseEntity
{
    public Guid ServiceCategoryId { get; set; }
    public ServiceCategory ServiceCategory { get; set; } = null!;
    
    public string SkuCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public string UnitType { get; set; } = "Flat"; // e.g., Flat, Hourly, SqFt
}
