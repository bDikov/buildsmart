using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Application.DTOs;

namespace BuildSmart.Core.Application.Interfaces;

public interface IAiService
{
    /// <summary>
    /// Generates a detailed scope of work and tasks based on the job's questions and answers,
    /// mapped against a list of allowed SKUs.
    /// </summary>
    Task<AiScopeBreakdownResponse> GenerateJobScopeAsync(JobPost jobPost, string humanReadableContext, List<ServiceSku> allowedSkus, string languageCode = "en", CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates the user-approved tasks and maps them to allowed SKUs, determining quantities and calculating prices.
    /// </summary>
    Task<AiTaskPricingResponse> CalculateTaskPricesAsync(List<JobTask> tasks, List<ServiceSku> allowedSkus, string humanReadableContext, string languageCode = "en", CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a high-level summary for the entire project.
    /// </summary>
    Task<string> GenerateProjectSummaryAsync(Project project, string languageCode = "en", CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a short, professional executive summary based on the combined scopes.
    /// </summary>
    Task<string> GenerateExecutiveSummaryAsync(string combinedScopes, string languageCode = "en", CancellationToken cancellationToken = default);
}
