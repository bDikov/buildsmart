using BuildSmart.Core.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BuildSmart.Api.Controllers;

[ApiController]
[Route("api/infra")]
public class InfraController : ControllerBase
{
    private readonly IInfraService _infraService;
    private readonly ILogger<InfraController> _logger;

    public InfraController(
        IInfraService infraService,
        ILogger<InfraController> logger)
    {
        _infraService = infraService;
        _logger = logger;
    }

    /// <summary>
    /// Returns live system health, database latency, and memory metrics.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var status = await _infraService.GetSystemStatusAsync(HttpContext.RequestAborted);
        return Ok(status);
    }

    /// <summary>
    /// Webhook endpoint for Sentry alerts.
    /// Processes exceptions, performs AI diagnosis, and sends Telegram alert with Auto-Fix actions.
    /// </summary>
    [HttpPost("sentry-webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> ReceiveSentryWebhook()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawJson = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return BadRequest(new { error = "Empty webhook payload" });
        }

        _logger.LogInformation("[InfraController] Received Sentry webhook incident alert.");
        var success = await _infraService.ProcessSentryWebhookAsync(rawJson, HttpContext.RequestAborted);

        return success ? Ok(new { status = "processed" }) : StatusCode(500, new { error = "Failed to process Sentry alert" });
    }

    /// <summary>
    /// Retrieves recent server application logs.
    /// </summary>
    [HttpGet("logs")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetLogs([FromQuery] int lines = 50)
    {
        var logs = await _infraService.GetRecentLogsAsync(lines, HttpContext.RequestAborted);
        return Ok(new { logs });
    }

    /// <summary>
    /// Restarts the API or specified container.
    /// </summary>
    [HttpPost("restart")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> Restart([FromQuery] string service = "api")
    {
        _logger.LogWarning("[InfraController] Admin initiated service restart for: {Service}", service);
        await _infraService.RestartServiceAsync(service, HttpContext.RequestAborted);
        return Ok(new { message = $"Service {service} restart initiated." });
    }

    /// <summary>
    /// Triggers the Self-Healing GitHub Actions workflow for a specified issue.
    /// </summary>
    [HttpPost("trigger-fix")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> TriggerFix([FromBody] TriggerFixRequest request)
    {
        _logger.LogInformation("[InfraController] Admin triggered Self-Healing for: {Title} at {FilePath}:{LineNumber}",
            request.ErrorTitle, request.FilePath, request.LineNumber);

        var success = await _infraService.TriggerSelfHealingWorkflowAsync(
            request.ErrorTitle,
            request.ErrorMessage,
            request.StackTrace,
            request.FilePath,
            request.LineNumber,
            HttpContext.RequestAborted);

        return success 
            ? Ok(new { status = "dispatched" }) 
            : StatusCode(500, new { error = "Failed to dispatch self-healing workflow" });
    }
}

public class TriggerFixRequest
{
    public string ErrorTitle { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? FilePath { get; set; }
    public int? LineNumber { get; set; }
}
