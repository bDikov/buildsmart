
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
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("Signin") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("apple-login")]
        public IActionResult AppleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("Signin") };
            return Challenge(properties, AppleAuthenticationDefaults.AuthenticationScheme);
        }

        [HttpGet("signin")]
        public async Task<IActionResult> Signin()
        {
            var result = await HttpContext.AuthenticateAsync();
            if (result?.Succeeded != true)
            {
                return BadRequest("External authentication failed.");
            }

            var principal = result.Principal;
            if (principal == null)
            {
                return BadRequest("External authentication failed.");
            }

            var email = principal.FindFirstValue(ClaimTypes.Email);
            if (email == null)
            {
                return BadRequest("Email not found in external authentication provider.");
            }

            var name = principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

            // At this point, you would typically find or create a user in your database
            // and generate a JWT token for them.
            var token = await _authService.GenerateJwtTokenForExternalLogin(email, name);

            return Ok(new { Token = token });
        }
    }
}
