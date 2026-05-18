using Microsoft.AspNetCore.Mvc;

namespace BuildSmart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly ILogger<DebugController> _logger;

    public DebugController(ILogger<DebugController> logger)
    {
        _logger = logger;
    }

    [HttpGet("test-error")]
    public IActionResult TestError()
    {
        _logger.LogInformation("Testing Sentry: This is an information message.");
        _logger.LogWarning("Testing Sentry: This is a warning message.");
        
        try
        {
            throw new Exception("Sentry Test: This is a manual crash to verify the integration!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sentry Test: Caught and logged an error.");
            throw; // Re-throw so Sentry Middleware catches it as an unhandled exception too
        }
    }
}
