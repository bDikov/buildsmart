using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class ServiceCategoryTranslation : BaseEntity
{
    public Guid CategoryId { get; set; }
    public ServiceCategory Category { get; set; } = null!;
    
    public string LanguageCode { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}