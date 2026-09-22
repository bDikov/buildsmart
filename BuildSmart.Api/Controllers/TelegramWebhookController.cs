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

    public TelegramWebhookController(
        IConfiguration configuration,
        IProjectChatService projectChatService,
        IUnitOfWork unitOfWork,
        IAiService aiService,
        ILogger<TelegramWebhookController> logger)
    {
        _configuration = configuration;
        _projectChatService = projectChatService;
        _unitOfWork = unitOfWork;
        _aiService = aiService;
        _logger = logger;
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

            if (!root.TryGetProperty("message", out var messageElement))
            {
                return Ok();
            }

            // Check if this message is a Reply to a previous notification
            if (messageElement.TryGetProperty("reply_to_message", out var replyToMessageElement) &&
                messageElement.TryGetProperty("text", out var adminReplyTextProp))
            {
                var adminReplyText = adminReplyTextProp.GetString();
                if (string.IsNullOrWhiteSpace(adminReplyText))
                {
                    return Ok();
                }

                // Check text or caption of the replied message for the PID:guid pattern
                string? originalText = null;
                if (replyToMessageElement.TryGetProperty("text", out var originalTextProp))
                {
                    originalText = originalTextProp.GetString();
                }
                else if (replyToMessageElement.TryGetProperty("caption", out var captionProp))
                {
                    originalText = captionProp.GetString();
                }

                if (!string.IsNullOrEmpty(originalText))
                {
                    var match = Regex.Match(originalText, @"PID:([0-9a-fA-F-]{36})");
                    if (match.Success && Guid.TryParse(match.Groups[1].Value, out var projectId))
                    {
                        // Find admin user to attribute the message
                        var adminUser = await _unitOfWork.Users.GetQueryable()
                            .FirstOrDefaultAsync(u => u.Role == UserRoleTypes.Admin);

                        if (adminUser != null)
                        {
                            _logger.LogInformation("[TelegramWebhook] Forwarding reply from Telegram to Project {ProjectId}", projectId);
                            await _projectChatService.SendMessageAsync(projectId, adminUser.Id, adminReplyText.Trim());
                        }
                    }
                }
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
