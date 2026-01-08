using BuildSmart.Core.Domain.Common;
using BuildSmart.Core.Domain.Enums;

namespace BuildSmart.Core.Domain.Entities.JoinEntities;

// Join entity for Many-to-Many between TradesmanProfile and ServiceCategory
public class TradesmanSkill : BaseEntity
{
    public Guid TradesmanProfileId { get; set; }
    public TradesmanProfile TradesmanProfile { get; set; } = null!;

    public Guid ServiceCategoryId { get; set; }
    public ServiceCategory ServiceCategory { get; set; } = null!;

    public SkillVerificationStatus VerificationStatus { get; set; } = SkillVerificationStatus.Unverified;
    
    // Optional: Years of experience in this specific skill
    public int YearsOfExperience { get; set; }
}
