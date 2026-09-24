using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BuildSmart.Api.Controllers;

[ApiController]
[Route("api/telegram")]
public class TelegramWebhookController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IProjectChatService _projectChatService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAiService _aiService;
    private readonly ILogger<TelegramWebhookController> _logger;
    private readonly IServiceProvider? _serviceProvider;

    public TelegramWebhookController(
        IConfiguration configuration,
        IProjectChatService projectChatService,
        IUnitOfWork unitOfWork,
        IAiService aiService,
        ILogger<TelegramWebhookController> logger,
        IServiceProvider? serviceProvider = null)
    {
        _configuration = configuration;
        _projectChatService = projectChatService;
        _unitOfWork = unitOfWork;
        _aiService = aiService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var isEnabled = bool.TryParse(_configuration["Telegram:Enabled"], out var enabled) && enabled;
        var hasToken = !string.IsNullOrWhiteSpace(_configuration["Telegram:BotToken"]);
        var hasChatId = !string.IsNullOrWhiteSpace(_configuration["Telegram:ChatId"]);

        return Ok(new
        {
            enabled = isEnabled,
            configured = hasToken && hasChatId
        });
    }

    /// <summary>
    /// Receives Telegram updates. When the admin replies to a notification message,
    /// it parses the ProjectId from the replied message and posts the text as an Admin message in the web chat.
    private static Guid? _lastActiveProjectId;

    public static void SetLastActiveProjectId(Guid projectId)
    {
        _lastActiveProjectId = projectId;
    }

    /// <summary>
    /// Receives Telegram updates via webhook. When the admin replies to a notification message,
    /// it parses the ProjectId and posts the text as an Admin message in the web chat.
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> ReceiveWebhook()
    {
        var configuredSecret = _configuration["Telegram:WebhookSecretToken"];
        if (!string.IsNullOrWhiteSpace(configuredSecret))
        {
            if (!Request.Headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var headerSecret) ||
                headerSecret != configuredSecret)
            {
                _logger.LogWarning("[TelegramWebhook] Received request with invalid or missing secret token.");
                return Forbid();
            }
        }

        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var rawJson = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return Ok();
            }

            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("message", out var messageElement))
            {
                await DispatchTelegramMessageAsync(messageElement, _unitOfWork, _projectChatService, _logger, _serviceProvider);
            }
            else if (root.TryGetProperty("callback_query", out var callbackQueryElement) && _serviceProvider != null)
            {
                await DispatchCallbackQueryAsync(callbackQueryElement, _serviceProvider, _logger);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TelegramWebhook] Error processing incoming Telegram update.");
            return Ok(); // Return 200 to prevent Telegram from retrying failed malformed updates indefinitely
        }
    }

    /// <summary>
    /// Processes an incoming Telegram message element, extracts the PID, and routes it to ProjectChatService.
    /// Shared between webhook execution and background polling.
    /// </summary>
    public static async Task<bool> DispatchTelegramMessageAsync(
        JsonElement messageElement,
        IUnitOfWork unitOfWork,
        IProjectChatService projectChatService,
        ILogger logger,
        IServiceProvider? serviceProvider = null)
    {
        if (!messageElement.TryGetProperty("text", out var adminReplyTextProp))
        {
            return false;
        }

        var adminReplyText = adminReplyTextProp.GetString();
        if (string.IsNullOrWhiteSpace(adminReplyText))
        {
            return false;
        }

        // Check text or caption of the replied message for the PID:guid pattern
        string? originalText = null;
        if (messageElement.TryGetProperty("reply_to_message", out var replyToMessageElement))
        {
            if (replyToMessageElement.TryGetProperty("text", out var originalTextProp))
            {
                originalText = originalTextProp.GetString();
            }
            else if (replyToMessageElement.TryGetProperty("caption", out var captionProp))
            {
                originalText = captionProp.GetString();
            }
        }

        Guid projectId = Guid.Empty;
        if (!string.IsNullOrEmpty(originalText))
        {
            var match = Regex.Match(originalText, @"PID:([0-9a-fA-F-]{36})");
            if (match.Success && Guid.TryParse(match.Groups[1].Value, out var parsedId))
            {
                projectId = parsedId;
                _lastActiveProjectId = parsedId;
            }
        }

        // Fallback: If no PID in reply quote, but we have a recently active project
        if (projectId == Guid.Empty && _lastActiveProjectId.HasValue)
        {
            projectId = _lastActiveProjectId.Value;
        }

        // Support bot control commands (e.g. /ai on, /ai off, /status, /restart, /logs, /fix, /help)
        if (adminReplyText.StartsWith("/"))
        {
            var command = adminReplyText.Trim().ToLowerInvariant();
            if (projectId != Guid.Empty)
            {
                if (command == "/ai on" || command == "/ai start")
                {
                    BuildSmart.Core.Application.Services.ProjectChatService.SetHumanTakeover(projectId, false);
                    logger.LogInformation("[Telegram] AI auto-reply resumed for Project {ProjectId}", projectId);
                    return true;
                }
                if (command == "/ai off" || command == "/ai stop" || command == "/ai pause")
                {
                    BuildSmart.Core.Application.Services.ProjectChatService.SetHumanTakeover(projectId, true, TimeSpan.FromHours(24));
                    logger.LogInformation("[Telegram] AI auto-reply paused for 24 hours for Project {ProjectId}", projectId);
                    return true;
                }
            }

            // Infrastructure / DevOps Commands
            if (serviceProvider != null)
            {
                var infraService = serviceProvider.GetService(typeof(IInfraService)) as IInfraService;
                var telegramBotService = serviceProvider.GetService(typeof(ITelegramBotService)) as ITelegramBotService;

                if (infraService != null && telegramBotService != null)
                {
                    if (command == "/status")
                    {
                        var st = await infraService.GetSystemStatusAsync();
                        var dbIcon = st.DatabaseConnected ? "✅ Свързана" : "❌ Прекъсната";
                        var uptimeStr = $"{st.Uptime.Days}д {st.Uptime.Hours}ч {st.Uptime.Minutes}м";
                        var msg = "<b>📊 СЪСТОЯНИЕ НА BUILDSMART ИНФРАСТРУКТУРА</b>\n\n" +
                                  $"• <b>Среда:</b> <code>{st.Environment}</code>\n" +
                                  $"• <b>База данни:</b> {dbIcon} (латентност: {st.DatabaseLatencyMs})\n" +
                                  $"• <b>RAM памет:</b> <code>{st.MemoryUsageMb} MB</code> (алокирана: {st.TotalAllocatedMb} MB)\n" +
                                  $"• <b>Uptime:</b> <code>{uptimeStr}</code>\n" +
                                  $"• <b>Хост:</b> <code>{st.OsVersion} ({st.ProcessorCount} CPU ядра)</code>\n" +
                                  $"• <b>Сървърно време (UTC):</b> <code>{st.ServerTimeUtc:yyyy-MM-dd HH:mm:ss}</code>";
                        await telegramBotService.SendNotificationAsync(msg);
                        return true;
                    }
                    if (command.StartsWith("/restart"))
                    {
                        await telegramBotService.SendNotificationAsync("🔄 <b>Иницииран е рестарт на API контейнера...</b>");
                        await infraService.RestartServiceAsync("api");
                        return true;
                    }
                    if (command.StartsWith("/logs"))
                    {
                        var parts = adminReplyText.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        string? filter = null;
                        int count = 15;
                        if (parts.Length > 1)
                        {
                            for (int i = 1; i < parts.Length; i++)
                            {
                                if (int.TryParse(parts[i], out var parsedCount))
                                {
                                    count = Math.Clamp(parsedCount, 5, 50);
                                }
                                else
                                {
                                    filter = parts[i].ToLowerInvariant();
                                }
                            }
                        }

                        var logs = await infraService.GetRecentLogsAsync(count, filter);
                        var filterLabel = string.IsNullOrEmpty(filter) ? "" : $" ({filter.ToUpperInvariant()})";
                        var snippet = logs.Length > 3800 ? logs.Substring(logs.Length - 3800) : logs;
                        await telegramBotService.SendNotificationAsync($"<b>📜 ПОСЛЕДНИ ЛОГОВЕ{filterLabel}:</b>\n<pre>{System.Web.HttpUtility.HtmlEncode(snippet)}</pre>");
                        return true;
                    }
                    if (command == "/fix")
                    {
                        await infraService.TriggerSelfHealingWorkflowAsync(
                            "Manual Admin Trigger",
                            "Triggered via Telegram /fix command",
                            null,
                            null,
                            null);
                        return true;
                    }
                    if (command == "/help")
                    {
                        var help = "<b>🤖 BUILDSMART BOT КОМАНДИ:</b>\n\n" +
                                   "<b>Чат с клиенти:</b>\n" +
                                   "• <code>Reply</code>: отговор към клиента в уеб чата\n" +
                                   "• <code>/ai on</code>: възобновяване на AI консултанта\n" +
                                   "• <code>/ai off</code>: спиране на AI за 24 часа\n\n" +
                                   "<b>Инфраструктура & DevOps:</b>\n" +
                                   "• <code>/status</code>: здраве на сървъра, RAM и базата\n" +
                                   "• <code>/restart</code>: рестартиране на API контейнера\n" +
                                   "• <code>/logs</code> [error | chat | N]: преглед на логове от Axiom\n" +
                                   "• <code>/fix</code>: задействане на Self-Healing GitHub Action";
                        await telegramBotService.SendNotificationAsync(help);
                        return true;
                    }
                }
            }

            logger.LogInformation("[Telegram] Ignoring bot command message: {Command}", adminReplyText);
            return false;
        }

        if (projectId == Guid.Empty)
        {
            logger.LogWarning("[Telegram] Could not determine ProjectId for incoming reply: '{Text}'", adminReplyText);
            return false;
        }

        // Find admin user to attribute the message
        var adminUser = await unitOfWork.Users.GetQueryable()
            .FirstOrDefaultAsync(u => u.Role == UserRoleTypes.Admin);

        adminUser ??= await unitOfWork.Users.GetQueryable().FirstOrDefaultAsync();

        if (adminUser != null)
        {
            logger.LogInformation("[Telegram] Forwarding reply from Telegram to Project {ProjectId}: '{Text}'", projectId, adminReplyText.Trim());
            await projectChatService.SendMessageAsync(projectId, adminUser.Id, adminReplyText.Trim());
            return true;
        }
        else
        {
            logger.LogWarning("[Telegram] No admin user found in database to attribute reply message.");
            return false;
        }
    }

    /// <summary>
    /// Handles Telegram inline keyboard button clicks (e.g. [Auto-Fix & Create PR], [Restart API], [Logs]).
    /// </summary>
    public static async Task<bool> DispatchCallbackQueryAsync(
        JsonElement callbackQueryElement,
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        if (!callbackQueryElement.TryGetProperty("data", out var dataProp))
            return false;

        var data = dataProp.GetString() ?? string.Empty;
        var infraService = serviceProvider.GetService(typeof(IInfraService)) as IInfraService;
        var telegramBotService = serviceProvider.GetService(typeof(ITelegramBotService)) as ITelegramBotService;

        if (infraService == null || telegramBotService == null) return false;

        if (data.StartsWith("fix:"))
        {
            var targetFile = data.Substring(4);
            logger.LogInformation("[TelegramCallback] Auto-Fix requested for file: {File}", targetFile);
            await infraService.TriggerSelfHealingWorkflowAsync(
                "Sentry Incident",
                "Auto-Fix triggered via Telegram inline button",
                null,
                targetFile,
                null);
            return true;
        }
        if (data == "restart:api")
        {
            logger.LogInformation("[TelegramCallback] API container restart requested via callback.");
            await telegramBotService.SendNotificationAsync("🔄 <b>Иницииран е рестарт на API контейнера...</b>");
            await infraService.RestartServiceAsync("api");
            return true;
        }
        if (data == "logs:api")
        {
            var logs = await infraService.GetRecentLogsAsync(15);
            var snippet = logs.Length > 3800 ? logs.Substring(logs.Length - 3800) : logs;
            await telegramBotService.SendNotificationAsync($"<b>📜 ПОСЛЕДНИ ЛОГОВЕ:</b>\n<pre>{System.Web.HttpUtility.HtmlEncode(snippet)}</pre>");
            return true;
        }

        return false;
    }


    /// <summary>
    /// Generates an AI-suggested draft reply for admins in the chat view.
    /// </summary>
    [HttpPost("suggest-reply")]
    [Authorize]
    public async Task<IActionResult> SuggestReply([FromBody] SuggestReplyRequest request)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(request.ProjectId);
        if (project == null)
        {
            return NotFound(new { error = "Project not found" });
        }

        var messages = await _unitOfWork.ProjectMessages.GetMessagesPaginatedAsync(request.ProjectId, 0, 5);
        var lastClientMessage = messages.FirstOrDefault(m => m.SenderId == project.HomeownerId)?.MessageText 
            ?? "Моля за повече информация за офертата.";

        var context = $"Проект: {project.Title}. Описание: {project.Description}.";
        var suggestion = await _aiService.GenerateChatReplyAsync(
            context,
            lastClientMessage,
            project.LanguageCode ?? "bg"
        );

        return Ok(new { suggestion });
    }
}

public class SuggestReplyRequest
{
    public Guid ProjectId { get; set; }
}
