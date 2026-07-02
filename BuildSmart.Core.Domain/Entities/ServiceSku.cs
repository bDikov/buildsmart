using BuildSmart.Core.Domain.Common;
using System.Collections.Generic;

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
    
    // The mathematical formula used by the C# Pricing Engine (e.g., "global_total_sqm * 3.5")
    public string CalculationFormula { get; set; } = string.Empty; 
    
    public string? EnglishName { get; set; }
    public string? EnglishDescription { get; set; }
    public string? EnglishUnitType { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
