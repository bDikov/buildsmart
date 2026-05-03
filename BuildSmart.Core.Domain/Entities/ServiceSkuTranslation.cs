using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class ServiceSkuTranslation : BaseEntity
{
    public Guid SkuId { get; set; }
    public ServiceSku Sku { get; set; } = null!;
    
    public string LanguageCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string UnitType { get; set; } = string.Empty;
}