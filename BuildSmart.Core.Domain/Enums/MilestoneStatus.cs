namespace BuildSmart.Core.Domain.Enums;

public enum MilestoneStatus
{
    Pending,    // Waiting for task to be completed by tradesman
    Approved,   // Homeowner has approved the task completion
    Paid,       // Funds have been transferred to the tradesman
    Disputed    // Homeowner rejected the task completion or opened a dispute
}