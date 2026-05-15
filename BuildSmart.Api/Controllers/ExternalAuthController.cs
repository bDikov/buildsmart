
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using BuildSmart.Core.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.Google;
using AspNet.Security.OAuth.Apple;

namespace BuildSmart.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExternalAuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public ExternalAuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet("google-login")]
        public IActionResult GoogleLogin(string returnUrl = "buildsmart://auth")
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("Signin", new { returnUrl }) };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("apple-login")]
        public IActionResult AppleLogin(string returnUrl = "buildsmart://auth")
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("Signin", new { returnUrl }) };
            return Challenge(properties, AppleAuthenticationDefaults.AuthenticationScheme);
        }

        [HttpGet("signin")]
        public async Task<IActionResult> Signin(string returnUrl = "buildsmart://auth")
        {
            // CRITICAL: We MUST authenticate against "ExternalCookie", not the default JWT scheme!
            var result = await HttpContext.AuthenticateAsync("ExternalCookie");
            if (result?.Succeeded != true)
            {
                return BadRequest($"External authentication failed. Result: {result?.Failure?.Message ?? "No Principal"}");
            }

            var principal = result.Principal;
            if (principal == null)
            {
                return BadRequest("External authentication failed (Principal is null).");
            }

            var email = principal.FindFirstValue(ClaimTypes.Email);
            if (email == null)
            {
                return BadRequest("Email not found in external authentication provider.");
            }

            var name = principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
            
            // Extract the profile picture URL from the "picture" or "image" claim
            var picture = principal.FindFirstValue("picture") ?? principal.FindFirstValue("image");

            // Find or create user and generate JWT
            var token = await _authService.GenerateJwtTokenForExternalLogin(email, name, picture);

            // Clean up the temporary cookie
            await HttpContext.SignOutAsync("ExternalCookie");

            // Redirect back to the MAUI or Blazor Web App with the token
            return Redirect($"{returnUrl}?token={token}");
        }
    }
}
