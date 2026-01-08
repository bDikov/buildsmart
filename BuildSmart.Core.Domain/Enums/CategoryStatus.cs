namespace BuildSmart.Core.Domain.Enums;

public enum CategoryStatus
{
    // Newly created, not visible to users
    Draft,
    
    // Visible in the wizard for project creation
    Active,

    // Hidden, no longer in use
    Archived
}
