using BuildSmart.Core.Application.DTOs;
using BuildSmart.Core.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BuildSmart.Infrastructure.Services;

public class InfraService : IInfraService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HttpClient _httpClient;
    private readonly IAiService _aiService;
    private readonly ITelegramBotService _telegramBotService;
    private readonly ILogger<InfraService> _logger;

    private static readonly DateTime _appStartTime = DateTime.UtcNow;

    public InfraService(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        HttpClient httpClient,
        IAiService aiService,
        ITelegramBotService telegramBotService,
        ILogger<InfraService> logger)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _httpClient = httpClient;
        _aiService = aiService;
        _telegramBotService = telegramBotService;
        _logger = logger;
    }

    public async Task<InfraStatusDto> GetSystemStatusAsync(CancellationToken cancellationToken = default)
    {
        var status = new InfraStatusDto
        {
            Environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production",
            OsVersion = Environment.OSVersion.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            ServerTimeUtc = DateTime.UtcNow,
            Uptime = DateTime.UtcNow - _appStartTime
        };

        // Measure DB Connectivity & Latency
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var sw = Stopwatch.StartNew();
            var canConnect = await unitOfWork.Users.GetQueryable().AnyAsync(cancellationToken);
            sw.Stop();
            status.DatabaseConnected = true;
            status.DatabaseLatencyMs = $"{sw.ElapsedMilliseconds} ms";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[InfraService] Database connectivity check failed.");
            status.DatabaseConnected = false;
            status.DatabaseLatencyMs = "Failed";
        }

        // Memory Usage
        try
        {
            var process = Process.GetCurrentProcess();
            status.MemoryUsageMb = Math.Round((double)process.WorkingSet64 / (1024 * 1024), 1);
            status.TotalAllocatedMb = Math.Round((double)GC.GetTotalMemory(false) / (1024 * 1024), 1);
        }
        catch
        {
            status.MemoryUsageMb = 0;
            status.TotalAllocatedMb = 0;
        }

        return status;
    }

    public Task<string> GetRecentLogsAsync(int lineCount = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check for log files in typical locations
            var logDirs = new[] { "logs", "Logs", "app_data/logs" };
            foreach (var dir in logDirs)
            {
                if (Directory.Exists(dir))
                {
                    var latestFile = new DirectoryInfo(dir)
                        .GetFiles("*.log")
                        .OrderByDescending(f => f.LastWriteTimeUtc)
                        .FirstOrDefault();

                    if (latestFile != null)
                    {
                        var lines = File.ReadLines(latestFile.FullName)
                            .TakeLast(lineCount);
                        return Task.FromResult(string.Join(Environment.NewLine, lines));
                    }
                }
            }

            return Task.FromResult($"[InfraService] No local log files found. Use 'docker logs buildsmart-api --tail {lineCount}' on VPS.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[InfraService] Error reading logs.");
            return Task.FromResult($"[InfraService] Error retrieving logs: {ex.Message}");
        }
    }

    public Task<bool> RestartServiceAsync(string serviceName = "api", CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("[InfraService] Graceful restart requested for service: {Service}", serviceName);

        // Schedule an asynchronous graceful exit in 1 second to allow HTTP response to flush
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000);
            _logger.LogInformation("[InfraService] Terminating process for Docker container restart policy (restart: always)...");
            Environment.Exit(0);
        });

        return Task.FromResult(true);
    }

    public async Task<bool> TriggerSelfHealingWorkflowAsync(
        string errorTitle,
        string errorMessage,
        string? stackTrace,
        string? filePath,
        int? lineNumber,
        CancellationToken cancellationToken = default)
    {
        var githubToken = _configuration["GitHub:PatToken"] 
            ?? _configuration["GITHUB_PAT"] 
            ?? Environment.GetEnvironmentVariable("GITHUB_PAT");

        var repoOwner = _configuration["GitHub:RepoOwner"] ?? "bDikov";
        var repoName = _configuration["GitHub:RepoName"] ?? "buildsmart";

        if (string.IsNullOrWhiteSpace(githubToken))
        {
            _logger.LogWarning("[InfraService] Cannot trigger Self-Healing workflow: GitHub:PatToken is not configured.");
            await _telegramBotService.SendNotificationAsync(
                "⚠️ <b>Self-Healing не може да стартира:</b> Липсва <code>GitHub:PatToken</code> в конфигурацията.\n" +
                "Моля, добавете GitHub Personal Access Token в User Secrets или средата.",
                cancellationToken: cancellationToken);
            return false;
        }

        try
        {
            var url = $"https://api.github.com/repos/{repoOwner}/{repoName}/dispatches";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("BuildSmart-InfraBot", "1.0"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            var payload = new
            {
                event_type = "self-healing-fix",
                client_payload = new
                {
                    error_title = errorTitle,
                    error_message = errorMessage,
                    stack_trace = stackTrace ?? string.Empty,
                    file_path = filePath ?? string.Empty,
                    line_number = lineNumber?.ToString() ?? string.Empty,
                    timestamp = DateTime.UtcNow.ToString("o")
                }
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[InfraService] Successfully triggered Self-Healing workflow on GitHub Actions!");
                await _telegramBotService.SendNotificationAsync(
                    $"🚀 <b>Self-Healing Агентът е задействан в GitHub Actions!</b>\n" +
                    $"• <b>Проблем:</b> <code>{EscapeHtml(errorTitle)}</code>\n" +
                    $"• <b>Файл:</b> <code>{EscapeHtml(filePath)}:{lineNumber}</code>\n" +
                    $"<i>Агентът подготвя фикс, изпълнява тестовете и ще изпрати линк към готовия Pull Request.</i>",
                    cancellationToken: cancellationToken);
                return true;
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("[InfraService] GitHub dispatch failed with {StatusCode}: {Body}", response.StatusCode, errorBody);
                await _telegramBotService.SendNotificationAsync(
                    $"⚠️ GitHub API върна грешка {response.StatusCode} при опит за стартиране на Self-Healing.",
                    cancellationToken: cancellationToken);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[InfraService] Exception while triggering GitHub Self-Healing workflow.");
            return false;
        }
    }

    public async Task<bool> ProcessSentryWebhookAsync(string webhookJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(webhookJson)) return false;

        try
        {
            using var doc = JsonDocument.Parse(webhookJson);
            var root = doc.RootElement;

            string errorTitle = "Application Exception";
            string errorMessage = "Unhandled error detected.";
            string? stackTrace = null;
            string? filePath = null;
            int? lineNumber = null;

            // Sentry webhook structure parser
            if (root.TryGetProperty("data", out var dataElement))
            {
                if (dataElement.TryGetProperty("issue", out var issueElement))
                {
                    if (issueElement.TryGetProperty("title", out var titleProp))
                        errorTitle = titleProp.GetString() ?? errorTitle;
                    if (issueElement.TryGetProperty("culprit", out var culpritProp))
                        errorMessage = culpritProp.GetString() ?? errorMessage;
                }

                if (dataElement.TryGetProperty("event", out var eventElement))
                {
                    if (eventElement.TryGetProperty("entries", out var entries) && entries.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var entry in entries.EnumerateArray())
                        {
                            if (entry.TryGetProperty("data", out var entryData) &&
                                entryData.TryGetProperty("values", out var values) &&
                                values.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var val in values.EnumerateArray())
                                {
                                    if (val.TryGetProperty("stacktrace", out var st) &&
                                        st.TryGetProperty("frames", out var frames) &&
                                        frames.ValueKind == JsonValueKind.Array)
                                    {
                                        var lastFrame = frames.EnumerateArray().LastOrDefault();
                                        if (lastFrame.TryGetProperty("filename", out var fnProp))
                                            filePath = fnProp.GetString();
                                        if (lastFrame.TryGetProperty("lineNo", out var lnProp))
                                            lineNumber = lnProp.GetInt32();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (root.TryGetProperty("message", out var msgProp))
            {
                errorMessage = msgProp.GetString() ?? errorMessage;
            }

            // Fallback: extract file and line from stackTrace if not found
            if (string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(stackTrace))
            {
                var match = Regex.Match(stackTrace, @"in ([\w\/\.\\]+\.cs):line (\d+)");
                if (match.Success)
                {
                    filePath = match.Groups[1].Value;
                    if (int.TryParse(match.Groups[2].Value, out var parsedLine))
                        lineNumber = parsedLine;
                }
            }

            // Quick AI Triage (2-3 sentences explanation)
            string aiTriage = string.Empty;
            try
            {
                var prompt = $"Analyze this C# exception briefly (in Bulgarian, 2 sentences, no emojis, purely technical):\nError: {errorTitle} - {errorMessage}\nLocation: {filePath}:{lineNumber}\nSuggest root cause and immediate remedy.";
                aiTriage = await _aiService.GenerateChatReplyAsync("DevOps Error Triage Assistant", prompt, "bg", cancellationToken);
            }
            catch
            {
                aiTriage = "Неуспешен AI анализ на грешката.";
            }

            // Format alert to Telegram
            var sb = new StringBuilder();
            sb.AppendLine("🚨 <b>КРИТИЧЕН АЛЕРТ ОТ SENTRY</b>");
            sb.AppendLine($"<b>Грешка:</b> <code>{EscapeHtml(errorTitle)}</code>");
            sb.AppendLine($"<b>Съобщение:</b> <i>{EscapeHtml(errorMessage)}</i>");
            if (!string.IsNullOrEmpty(filePath))
            {
                sb.AppendLine($"<b>Локация:</b> <code>{EscapeHtml(filePath)}:{lineNumber}</code>");
            }
            if (!string.IsNullOrWhiteSpace(aiTriage))
            {
                sb.AppendLine();
                sb.AppendLine("<b>AI Анализ:</b>");
                sb.AppendLine(EscapeHtml(aiTriage));
            }

            var inlineButtons = new List<object>();
            if (!string.IsNullOrEmpty(filePath))
            {
                // Trigger Self-Healing PR
                inlineButtons.Add(new { text = "🛠️ Auto-Fix & Create PR", callback_data = $"fix:{EscapeCallback(filePath)}" });
            }
            inlineButtons.Add(new { text = "🔄 Рестарт API", callback_data = "restart:api" });
            inlineButtons.Add(new { text = "📜 Логове", callback_data = "logs:api" });

            var replyMarkup = new
            {
                inline_keyboard = new[] { inlineButtons.ToArray() }
            };

            await _telegramBotService.SendNotificationAsync(sb.ToString(), replyMarkup, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[InfraService] Error parsing Sentry webhook payload.");
            return false;
        }
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

    private static string EscapeCallback(string input)
    {
        if (input.Length > 40) return input.Substring(input.Length - 40);
        return input;
    }
}
