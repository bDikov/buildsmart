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
    public bool IsContacted { get; set; } = false;
    public DateTime? ContactedAt { get; set; }
    public string? AdminNotes { get; set; }

    // CRM Call Assessment & Prospect Rating
    public string? CallOutcome { get; set; } // "WillingToWork", "Possible", "Reserved", "NotPossible", null
    public int? ProspectRating { get; set; } // 1 to 10 scale (10 = highest perspective, 1 = pointless/not possible)

    // CRM Live Meeting & Site Visit
    public string? MeetingStatus { get; set; } // "None", "Requested", "Scheduled", "Completed", "Cancelled"
    public DateTime? MeetingDate { get; set; }
    public string? MeetingNotes { get; set; }

    // CRM Client Readiness & Follow-up Reminders
    public string? ReadyToStartTimeline { get; set; } // e.g. "immediately", "1_month", "2_3_months", "6_months_plus", "act_16", "custom"
    public DateTime? FollowUpDate { get; set; }
    public bool FollowUpReminderSent { get; set; } = false;
}

