using BuildSmart.Core.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BuildSmart.Infrastructure.Services;

public class TelegramBotService : ITelegramBotService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramBotService> _logger;

    private readonly string? _botToken;
    private readonly string? _adminChatId;
    private readonly bool _isEnabled;
    private readonly string _appBaseUrl;

    public TelegramBotService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TelegramBotService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _botToken = configuration["Telegram:BotToken"];
        _adminChatId = configuration["Telegram:ChatId"];
        _isEnabled = bool.TryParse(configuration["Telegram:Enabled"], out var enabled) && enabled;
        _appBaseUrl = configuration["AppBaseUrl"]?.TrimEnd('/') ?? "https://buildsmart.bg";
    }

    public async Task<bool> SendNotificationAsync(string message, object? replyMarkup = null, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled || string.IsNullOrWhiteSpace(_botToken) || string.IsNullOrWhiteSpace(_adminChatId))
        {
            _logger.LogInformation("[TelegramBotService] Skipping Telegram notification because Telegram integration is not enabled or credentials are missing.");
            return false;
        }

        try
        {
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
            var payload = new Dictionary<string, object?>
            {
                ["chat_id"] = _adminChatId,
                ["text"] = message,
                ["parse_mode"] = "HTML"
            };

            if (replyMarkup != null)
            {
                payload["reply_markup"] = replyMarkup;
            }

            var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("[TelegramBotService] Telegram API failed with status {StatusCode}: {Error}", response.StatusCode, error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TelegramBotService] Exception occurred while sending Telegram notification.");
            return false;
        }
    }

    public async Task<bool> SendLeadAlertAsync(
        Guid projectId,
        string projectTitle,
        string? homeownerName,
        string? homeownerPhone,
        string? homeownerEmail,
        string? location,
        string? aiSummary = null,
        CancellationToken cancellationToken = default)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>НОВ ЛИЙД / ПРОЕКТ В BUILDSMART</b>");
        sb.AppendLine($"<b>Проект:</b> {EscapeHtml(projectTitle)}");
        if (!string.IsNullOrWhiteSpace(homeownerName))
            sb.AppendLine($"<b>Клиент:</b> {EscapeHtml(homeownerName)}");
        if (!string.IsNullOrWhiteSpace(homeownerPhone))
            sb.AppendLine($"<b>Телефон:</b> {EscapeHtml(homeownerPhone)}");
        if (!string.IsNullOrWhiteSpace(homeownerEmail))
            sb.AppendLine($"<b>Имейл:</b> {EscapeHtml(homeownerEmail)}");
        if (!string.IsNullOrWhiteSpace(location))
            sb.AppendLine($"<b>Локация:</b> {EscapeHtml(location)}");

        if (!string.IsNullOrWhiteSpace(aiSummary))
        {
            sb.AppendLine();
            sb.AppendLine("<b>AI Анализ:</b>");
            sb.AppendLine(EscapeHtml(aiSummary));
        }

        sb.AppendLine();
        sb.AppendLine($"<code>PID:{projectId}</code>");

        var inlineButtons = new List<object>();

        if (!string.IsNullOrWhiteSpace(homeownerPhone))
        {
            var cleanPhone = CleanPhoneNumber(homeownerPhone);
            inlineButtons.Add(new { text = "Пиши по WhatsApp", url = $"https://wa.me/{cleanPhone}" });
        }

        inlineButtons.Add(new { text = "Преглед в Админ", url = $"{_appBaseUrl}/project-messages?projectId={projectId}" });

        var replyMarkup = new
        {
            inline_keyboard = new[] { inlineButtons.ToArray() }
        };

        return await SendNotificationAsync(sb.ToString(), replyMarkup, cancellationToken);
    }

    public async Task<bool> SendChatMessageAlertAsync(
        Guid projectId,
        string projectTitle,
        string senderName,
        string messageText,
        string? senderPhone = null,
        CancellationToken cancellationToken = default)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>НОВО СЪОБЩЕНИЕ В ЧАТА</b>");
        sb.AppendLine($"<b>Проект:</b> {EscapeHtml(projectTitle)}");
        sb.AppendLine($"<b>От:</b> {EscapeHtml(senderName)}");
        sb.AppendLine();
        sb.AppendLine($"<i>\"{EscapeHtml(messageText)}\"</i>");
        sb.AppendLine();
        sb.AppendLine($"<i>(Отговорете с 'Reply' на това съобщение в Telegram, за да изпратите отговор към клиента)</i>");
        sb.AppendLine($"<code>PID:{projectId}</code>");

        var inlineButtons = new List<object>();

        if (!string.IsNullOrWhiteSpace(senderPhone))
        {
            var cleanPhone = CleanPhoneNumber(senderPhone);
            inlineButtons.Add(new { text = "Пиши по WhatsApp", url = $"https://wa.me/{cleanPhone}" });
        }

        inlineButtons.Add(new { text = "Отвори Чат в Сайта", url = $"{_appBaseUrl}/project-messages?projectId={projectId}" });

        var replyMarkup = new
        {
            inline_keyboard = new[] { inlineButtons.ToArray() }
        };

        return await SendNotificationAsync(sb.ToString(), replyMarkup, cancellationToken);
    }

    public async Task<bool> SendAiReplyNotificationAsync(
        Guid projectId,
        string clientName,
        string aiReplyText,
        CancellationToken cancellationToken = default)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>AI Асистент отговори на клиента:</b>");
        sb.AppendLine($"<b>Към:</b> {EscapeHtml(clientName)}");
        sb.AppendLine();
        sb.AppendLine($"<i>\"{EscapeHtml(aiReplyText)}\"</i>");
        sb.AppendLine();
        sb.AppendLine($"<i>(Ако желаете да коригирате или допълните нещо, отговорете с 'Reply' тук)</i>");
        sb.AppendLine($"<code>PID:{projectId}</code>");

        var inlineButtons = new[]
        {
            new { text = "Отвори Чат в Сайта", url = $"{_appBaseUrl}/project-messages?projectId={projectId}" }
        };

        var replyMarkup = new
        {
            inline_keyboard = new[] { inlineButtons }
        };

        return await SendNotificationAsync(sb.ToString(), replyMarkup, cancellationToken);
    }

    private static string EscapeHtml(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }

    private static string CleanPhoneNumber(string phone)
    {
        // Strip out non-digits except leading +
        var cleaned = Regex.Replace(phone, @"[^\d+]", "");
        if (cleaned.StartsWith("00"))
        {
            cleaned = "+" + cleaned.Substring(2);
        }
        else if (cleaned.StartsWith("0") && !cleaned.StartsWith("+"))
        {
            cleaned = "+359" + cleaned.Substring(1);
        }
        return cleaned.Replace("+", "");
    }
}
