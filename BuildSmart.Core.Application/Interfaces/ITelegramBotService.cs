using System;
using System.Threading;
using System.Threading.Tasks;

namespace BuildSmart.Core.Application.Interfaces;

public interface ITelegramBotService
{
    /// <summary>
    /// Sends a general notification message with optional inline keyboard buttons to the configured admin Telegram chat.
    /// </summary>
    Task<bool> SendNotificationAsync(string message, object? replyMarkup = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an alert to Telegram when a new lead/project is submitted.
    /// Includes client contact info, project highlights, WhatsApp quick-link, and optional AI summary.
    /// </summary>
    Task<bool> SendLeadAlertAsync(
        Guid projectId,
        string projectTitle,
        string? homeownerName,
        string? homeownerPhone,
        string? homeownerEmail,
        string? location,
        string? aiSummary = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an alert to Telegram when a client sends a message in the web chat.
    /// Admin can reply directly to this message in Telegram to respond to the client.
    /// </summary>
    Task<bool> SendChatMessageAlertAsync(
        Guid projectId,
        string projectTitle,
        string senderName,
        string messageText,
        string? senderPhone = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an alert to Telegram notifying the admin of the AI's automated response to a client.
    /// </summary>
    Task<bool> SendAiReplyNotificationAsync(
        Guid projectId,
        string clientName,
        string aiReplyText,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an alert to Telegram notifying the admin of a production error or critical system event.
    /// Includes source/service name, error message, truncated stack trace, optional JobId, and direct action links.
    /// </summary>
    Task<bool> SendProductionAlertAsync(
        string source,
        string message,
        string? stackTrace = null,
        Guid? jobId = null,
        CancellationToken cancellationToken = default);
}
