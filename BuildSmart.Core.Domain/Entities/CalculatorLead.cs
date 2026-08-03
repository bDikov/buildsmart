using BuildSmart.Core.Domain.Common;

namespace BuildSmart.Core.Domain.Entities;

public class CalculatorLead : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Name { get; set; }
    public string Scope { get; set; } = "full"; // "full" or "bathroom"
    public int SelectedArea { get; set; }
    public string BuildingStatus { get; set; } = "bds"; // "bds", "rough", "old"
    public string QualityTier { get; set; } = "standard"; // "standard", "premium", "luxury"
    public bool IncludeFurniture { get; set; }
    public bool IncludeEquipment { get; set; }
    public int BathroomCount { get; set; } = 1;
    public decimal MinPriceEur { get; set; }
    public decimal MaxPriceEur { get; set; }
    public decimal MinPriceBgn { get; set; }
    public decimal MaxPriceBgn { get; set; }
    public int EstimatedDays { get; set; }
    public bool IsEmailVerified { get; set; }
    public string VerificationStatus { get; set; } = "Valid"; // "Valid", "DisposableDomain", "NoMxRecord", "InvalidSyntax"
    public string? VerificationReason { get; set; }
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
    public string? UtmTerm { get; set; }
    public string? UtmContent { get; set; }
}
