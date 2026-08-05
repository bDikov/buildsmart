using BuildSmart.Core.Domain.Enums;
using TaskStatus = BuildSmart.Core.Domain.Enums.TaskStatus;

namespace BuildSmart.Core.Application.DTOs;

public class UserLookupDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

public class ProjectKanbanBoardDto
{
    public Guid ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public Guid HomeownerId { get; set; }
    public string HomeownerName { get; set; } = string.Empty;
    public ProjectStatus ProjectStatus { get; set; }
    public decimal AdminMarkupPercentage { get; set; } = 20.0m;
    public List<CategoryKanbanSectionDto> CategorySections { get; set; } = new();
}

public class CategoryKanbanSectionDto
{
    public Guid JobPostId { get; set; }
    public Guid ServiceCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public Guid? AssignedTradesmanId { get; set; }
    public string? AssignedTradesmanName { get; set; }
    public List<KanbanTaskCardDto> Tasks { get; set; } = new();
}

public class KanbanTaskCardDto
{
    public Guid TaskId { get; set; }
    public Guid JobPostId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public decimal EstimatedPrice { get; set; } // Client Price (Homeowner)
    public decimal TradesmanPrice { get; set; } // Tradesman Base Price
    public decimal DisplayPrice { get; set; }   // Role-Tailored Price
    public TaskStatus Status { get; set; }
    public Guid? AssignedTradesmanId { get; set; }
    public string? AssignedTradesmanName { get; set; }
    public int SkuCount { get; set; }
    public int CommentCount { get; set; }
    public List<TaskSkuSummaryDto> Skus { get; set; } = new();
    public List<TaskAcceptanceCriteriaDto> AcceptanceCriteria { get; set; } = new();
    public List<TaskCommentDto> Comments { get; set; } = new();
    public TaskPaymentInfoDto? PaymentInfo { get; set; }
}

public class TaskSkuSummaryDto
{
    public Guid SkuId { get; set; }
    public string SkuCode { get; set; } = string.Empty;
    public string SkuTitle { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal BasePriceBgn { get; set; }
    public decimal TradesmanSubtotalEur { get; set; }
    public decimal SubtotalEur { get; set; }
}

public class TaskAcceptanceCriteriaDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class TaskCommentDto
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public UserRoleTypes AuthorRole { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsSystemNote { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class TaskPaymentInfoDto
{
    public Guid PaymentRecordId { get; set; }
    public PaymentStatus Status { get; set; }
    public decimal CalculatedAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaidByAdminName { get; set; }
    public string? PaymentNotes { get; set; }
}

public class ProjectPaymentsBoardDto
{
    public Guid ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public decimal AdminMarkupPercentage { get; set; } = 20.0m;
    public decimal TotalProjectValue { get; set; }          // Client Offer Value
    public decimal TotalTradesmanPayoutValue { get; set; }   // Tradesman Payout Value
    public decimal NetAdminMarginValue { get; set; }         // Admin Profit Margin
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalAwaitingAmount { get; set; }
    public decimal TotalUpcomingAmount { get; set; }
    public List<KanbanTaskCardDto> UpcomingTasks { get; set; } = new();
    public List<KanbanTaskCardDto> AwaitingPaymentTasks { get; set; } = new();
    public List<KanbanTaskCardDto> PaidTasks { get; set; } = new();
}

public class TaskSkuEditDto
{
    public Guid? SkuId { get; set; }
    public string SkuCode { get; set; } = string.Empty;
    public string SkuTitle { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal BasePriceBgn { get; set; }
}

public class CustomOfferPhaseDto
{
    public string PhaseTitle { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public List<CustomOfferItemDto> Items { get; set; } = new();
}

public class CustomOfferItemDto
{
    public string SkuCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Unit { get; set; } = "м²";
    public decimal Quantity { get; set; }
    public decimal UnitPriceEur { get; set; }
    public decimal TotalEur => Math.Round(Quantity * UnitPriceEur, 2);
}


