using System.Collections.Generic;

namespace BuildSmart.Core.Application.DTOs;

public record AiTaskPricingItemDto(
    Guid TaskId,
    string TaskTitle,
    List<AiTaskSkuItemDto> SkuItems
);

public record AiTaskPricingResponse(
    List<AiTaskPricingItemDto> Tasks
);