using BuildSmart.Core.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace BuildSmart.Core.Application.Interfaces;

public interface IInfraService
{
    /// <summary>
    /// Collects system health metrics (database connectivity, memory usage, uptime, Hangfire stats).
    /// </summary>
    Task<InfraStatusDto> GetSystemStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves recent application logs.
    /// </summary>
    Task<string> GetRecentLogsAsync(int lineCount = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves recent application logs with optional filter (e.g. 'error', 'chat', or keyword).
    /// </summary>
    Task<string> GetRecentLogsAsync(int lineCount, string? filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests a graceful service or container restart.
    /// </summary>
    Task<bool> RestartServiceAsync(string serviceName = "api", CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers the GitHub Actions Self-Healing pipeline (repository_dispatch) with error details.
    /// </summary>
    Task<bool> TriggerSelfHealingWorkflowAsync(
        string errorTitle,
        string errorMessage,
        string? stackTrace,
        string? filePath,
        int? lineNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes an incoming Sentry webhook payload, analyzes the crash with AI,
    /// and alerts the admin via Telegram with diagnostic explanation and quick action buttons.
    /// </summary>
    Task<bool> ProcessSentryWebhookAsync(string webhookJson, CancellationToken cancellationToken = default);
}
