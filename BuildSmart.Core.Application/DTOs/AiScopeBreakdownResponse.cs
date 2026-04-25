using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.DTOs;

public record AiTaskSkuItemDto(
    string SkuCode, 
    decimal Quantity
);

public record AiTaskBreakdownItem(
    string TaskTitle, 
    string TaskDescription, 
    List<AiTaskSkuItemDto> SkuItems,
    List<string> AcceptanceCriteria
);

public record AiScopeBreakdownResponse(
    string ScopeMarkdown, 
    List<AiTaskBreakdownItem> Tasks
);
