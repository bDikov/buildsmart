using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Application.DTOs;

namespace BuildSmart.Core.Application.Interfaces;

public interface IAiService
{
    /// <summary>
    /// Generates a detailed scope of work and tasks based on the job's questions and answers,
    /// mapped against a list of allowed SKUs.
    /// </summary>
    Task<AiScopeBreakdownResponse> GenerateJobScopeAsync(JobPost jobPost, string humanReadableContext, List<ServiceSku> allowedSkus);

    /// <summary>
    /// Generates a high-level summary for the entire project.
    /// </summary>
    Task<string> GenerateProjectSummaryAsync(Project project);
}
