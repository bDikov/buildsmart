namespace BuildSmart.Core.Domain.Enums;

public enum SkillVerificationStatus
{
    Unverified,
    
    // Level 1: Admin checked their photos/portfolio
    PortfolioVerified, 
    
    // Level 2: Automagically verified after X good reviews
    CommunityVerified,
    
    // Level 3: Official government license checked
    LicenseVerified
}
