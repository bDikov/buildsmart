using BuildSmart.Core.Application.DTOs;
using BuildSmart.Core.Domain.Enums;
using TaskStatus = BuildSmart.Core.Domain.Enums.TaskStatus;

namespace BuildSmart.Core.Application.Interfaces;

public interface IProjectManagementService
{
    Task<Guid> CreateProjectForUserAsync(Guid homeownerUserId, string title, string description, List<Guid> serviceCategoryIds, string? location, Dictionary<Guid, Guid>? categoryTradesmanMap = null, decimal adminMarkupPercentage = 20.0m);
    Task AssignTradesmanToCategoryAsync(Guid projectId, Guid jobPostId, Guid tradesmanUserId, Guid adminUserId);
    Task SetProjectStatusAsync(Guid projectId, ProjectStatus newStatus, Guid adminUserId);
    Task UpdateTaskStatusAsync(Guid taskId, TaskStatus newStatus, Guid currentUserId, string? rejectionReason);
    Task AddTaskCommentAsync(Guid taskId, Guid authorId, string content, List<string>? imageUrls);
    Task<ProjectKanbanBoardDto> GetProjectKanbanBoardAsync(Guid projectId, Guid currentUserId, UserRoleTypes role);
    Task<ProjectPaymentsBoardDto> GetProjectPaymentsBoardAsync(Guid projectId, Guid currentUserId, UserRoleTypes role);
    Task MarkTaskPaidAsync(Guid taskId, Guid adminUserId, decimal finalAmount, string? notes);
    Task<List<UserLookupDto>> GetHomeownersLookupAsync();
    Task<List<UserLookupDto>> GetTradesmenLookupAsync();
    Task<List<BuildSmart.Core.Domain.Entities.ServiceCategory>> GetTradeCategoriesAsync();
    Task DeleteTaskAsync(Guid taskId, Guid currentUserId);
    Task<Guid> CreateTaskAsync(Guid jobPostId, string title, string description, decimal estimatedPrice, Guid adminUserId);
    Task UpdateTaskDetailsAndSkusAsync(Guid taskId, string title, string description, decimal estimatedPrice, List<TaskSkuEditDto> skus, Guid adminUserId);
    Task UpdateProjectMarkupPercentageAsync(Guid projectId, decimal markupPercentage, Guid adminUserId);
    Task<Guid> CreateProjectFromOfferTemplateAsync(Guid homeownerUserId, string title, string description, string? location, Dictionary<Guid, Guid>? categoryTradesmanMap, decimal adminMarkupPercentage, List<CustomOfferPhaseDto> phases, List<Guid>? additionalCategoryIds = null);
    Task AddCategoryToProjectAsync(Guid projectId, Guid categoryId, Guid? assignedTradesmanId = null, Guid? adminUserId = null);
    Task ReorderTaskAsync(Guid taskId, int direction, Guid currentUserId);
    Task MoveTaskBeforeTaskAsync(Guid draggedTaskId, Guid targetTaskId, Guid currentUserId);
}

