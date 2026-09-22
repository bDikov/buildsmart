using BuildSmart.Api.Controllers;
using BuildSmart.Core.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BuildSmart.Api.Workers;

/// <summary>
/// Background worker that polls Telegram for incoming updates when running in local development
/// or when a public webhook URL is not configured.
/// This enables instant bidirectional replies between the admin in Telegram and the client in the web chat.
/// </summary>
public class TelegramPollingWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TelegramPollingWorker> _logger;

    public TelegramPollingWorker(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<TelegramPollingWorker> logger)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var isEnabled = bool.TryParse(_configuration["Telegram:Enabled"], out var enabled) && enabled;
        var botToken = _configuration["Telegram:BotToken"];

        if (!isEnabled || string.IsNullOrWhiteSpace(botToken))
        {
            _logger.LogInformation("[TelegramPollingWorker] Disabled or no bot token configured.");
            return;
        }

        // Brief delay to allow API and DB connection pool to initialize
        await Task.Delay(3000, stoppingToken);

        var httpClient = _httpClientFactory.CreateClient("TelegramPolling");
        httpClient.Timeout = TimeSpan.FromSeconds(25);
        long offset = 0;

        _logger.LogInformation("[TelegramPollingWorker] Started polling Telegram updates for replies...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Check if a public webhook URL is registered on Telegram's side.
                // If a webhook is active, Telegram rejects getUpdates with HTTP 409 Conflict.
                // In that case, we let the webhook handle updates and keep polling in standby.
                try
                {
                    var webhookInfoUrl = $"https://api.telegram.org/bot{botToken}/getWebhookInfo";
                    var webhookInfoResp = await httpClient.GetAsync(webhookInfoUrl, stoppingToken);
                    if (webhookInfoResp.IsSuccessStatusCode)
                    {
                        var infoJson = await webhookInfoResp.Content.ReadAsStringAsync(stoppingToken);
                        using var infoDoc = JsonDocument.Parse(infoJson);
                        if (infoDoc.RootElement.TryGetProperty("result", out var res) &&
                            res.TryGetProperty("url", out var urlProp) &&
                            !string.IsNullOrWhiteSpace(urlProp.GetString()))
                        {
                            // Active webhook detected (production environment)
                            await Task.Delay(30000, stoppingToken);
                            continue;
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogDebug("[TelegramPollingWorker] Webhook check notice: {Error}", ex.Message);
                }

                // Long polling: wait up to 10 seconds for new messages
                var getUpdatesUrl = $"https://api.telegram.org/bot{botToken}/getUpdates?offset={offset}&timeout=10";
                var response = await httpClient.GetAsync(getUpdatesUrl, stoppingToken);

                if (!response.IsSuccessStatusCode)
                {
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }

                var content = await response.Content.ReadAsStringAsync(stoppingToken);
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.TryGetProperty("result", out var updates) && updates.ValueKind == JsonValueKind.Array)
                {
                    foreach (var update in updates.EnumerateArray())
                    {
                        if (update.TryGetProperty("update_id", out var updateIdProp))
                        {
                            var updateId = updateIdProp.GetInt64();
                            offset = updateId + 1; // Acknowledge update to Telegram so it's not redelivered
                        }

                        if (update.TryGetProperty("message", out var messageElement))
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                            var chatService = scope.ServiceProvider.GetRequiredService<IProjectChatService>();

                            await TelegramWebhookController.DispatchTelegramMessageAsync(
                                messageElement,
                                uow,
                                chatService,
                                _logger);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[TelegramPollingWorker] Error while polling Telegram updates: {Message}", ex.Message);
                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}
