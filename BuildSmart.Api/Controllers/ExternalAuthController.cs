
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using BuildSmart.Core.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
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

        [HttpGet("facebook-login")]
        public IActionResult FacebookLogin(string returnUrl = "buildsmart://auth")
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("Signin", new { returnUrl }) };
            return Challenge(properties, FacebookDefaults.AuthenticationScheme);
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
            try
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
                if (string.IsNullOrEmpty(email))
                {
                    var id = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("id") ?? Guid.NewGuid().ToString("N");
                    email = $"{id}@facebook.user";
                }

                var name = principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
                
                // Extract the profile picture URL from the "picture" or "image" claim
                var picture = principal.FindFirstValue("picture") ?? principal.FindFirstValue("image");

                // Find or create user and generate JWT
                var (token, isNewUser) = await _authService.GenerateJwtTokenForExternalLogin(email, name, picture);

                // Clean up the temporary cookie
                await HttpContext.SignOutAsync("ExternalCookie");

                // Redirect back to the MAUI or Blazor Web App with the token
                var separator = returnUrl.Contains("?") ? "&" : "?";
                var redirectUrl = $"{returnUrl}{separator}token={token}";
                if (isNewUser)
                {
                    redirectUrl += "&isNewUser=true";
                }
                return Redirect(GetSafeRedirectUrl(redirectUrl));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}");
            }
        }

        private static string GetSafeRedirectUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return "buildsmart://auth";
            }

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < url.Length; i++)
            {
                char ch = url[i];
                if (ch <= 127)
                {
                    sb.Append(ch);
                }
                else
                {
                    if (char.IsHighSurrogate(ch) && i + 1 < url.Length && char.IsLowSurrogate(url[i + 1]))
                    {
                        var surrogateStr = url.Substring(i, 2);
                        var bytes = System.Text.Encoding.UTF8.GetBytes(surrogateStr);
                        foreach (var b in bytes)
                        {
                            sb.AppendFormat("%{0:X2}", b);
                        }
                        i++;
                    }
                    else
                    {
                        var bytes = System.Text.Encoding.UTF8.GetBytes(ch.ToString());
                        foreach (var b in bytes)
                        {
                            sb.AppendFormat("%{0:X2}", b);
                        }
                    }
                }
            }
            return sb.ToString();
        }
    }
}
