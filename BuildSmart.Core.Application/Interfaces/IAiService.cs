using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IAiService
{
    /// <summary>
    /// Generates a detailed scope of work based on the job's questions and answers.
    /// </summary>
    Task<string> GenerateJobScopeAsync(JobPost jobPost);

    /// <summary>
    /// Generates a high-level summary for the entire project.
    /// </summary>
    Task<string> GenerateProjectSummaryAsync(Project project);
}
