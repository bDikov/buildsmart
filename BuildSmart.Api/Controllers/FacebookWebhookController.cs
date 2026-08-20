using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BuildSmart.Api.Controllers
{
    [ApiController]
    [Route("api/facebook")]
    public class FacebookWebhookController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FacebookWebhookController> _logger;

        public FacebookWebhookController(IConfiguration configuration, ILogger<FacebookWebhookController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Meta Webhook Verification Handshake (GET).
        /// Triggered by Meta Developers Portal when adding/verifying Callback URL.
        /// </summary>
        [HttpGet("webhook")]
        public IActionResult VerifyWebhook(
            [FromQuery(Name = "hub.mode")] string? mode,
            [FromQuery(Name = "hub.verify_token")] string? verifyToken,
            [FromQuery(Name = "hub.challenge")] string? challenge)
        {
            _logger.LogInformation("[FacebookWebhook] Verification request received. Mode: {Mode}, Token: {Token}", mode, verifyToken);

            var expectedToken = _configuration["Authentication:Facebook:VerifyToken"] ?? "BuildSmart_FB_Webhook_Secret_Token_2026";

            if (!string.IsNullOrEmpty(mode) && mode == "subscribe" && verifyToken == expectedToken)
            {
                _logger.LogInformation("[FacebookWebhook] Verification successful. Returning hub.challenge.");
                return Content(challenge ?? string.Empty, "text/plain", Encoding.UTF8);
            }

            _logger.LogWarning("[FacebookWebhook] Verification failed. Token mismatch or invalid mode.");
            return Forbid();
        }

        /// <summary>
        /// Meta Real-Time Event Notification Endpoint (POST).
        /// Receives leadgen events, ad account updates, page interactions, etc.
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveWebhookNotification()
        {
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var rawJson = await reader.ReadToEndAsync();

                _logger.LogInformation("[FacebookWebhook] Notification received: {Payload}", rawJson);

                // Parse and process webhook payload
                if (!string.IsNullOrWhiteSpace(rawJson))
                {
                    using var doc = JsonDocument.Parse(rawJson);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("object", out var objectProp))
                    {
                        var objectType = objectProp.GetString();
                        _logger.LogInformation("[FacebookWebhook] Event object type: {ObjectType}", objectType);

                        if (root.TryGetProperty("entry", out var entryArray) && entryArray.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var entry in entryArray.EnumerateArray())
                            {
                                if (entry.TryGetProperty("changes", out var changesArray) && changesArray.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var change in changesArray.EnumerateArray())
                                    {
                                        if (change.TryGetProperty("field", out var fieldProp))
                                        {
                                            var fieldName = fieldProp.GetString();
                                            _logger.LogInformation("[FacebookWebhook] Webhook field changed: {FieldName}", fieldName);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Meta requires HTTP 200 OK within 20 seconds to acknowledge receipt
                return Ok(new { status = "success" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FacebookWebhook] Error processing webhook payload.");
                return Ok(new { status = "error", message = ex.Message });
            }
        }
    }
}
