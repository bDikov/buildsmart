using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace BuildSmart.SharedUI.Services
{
    public class MauiAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IAuthService _authService;

        public MauiAuthenticationStateProvider(IAuthService authService)
        {
            _authService = authService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var token = await _authService.GetTokenAsync();

                if (string.IsNullOrEmpty(token))
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // Check if the token has expired
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    Console.WriteLine("[Auth] Token has expired. Clearing session.");
                    await _authService.ClearTokenAsync();
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // Specify the role claim type so Blazor's <AuthorizeView Roles="..."> works correctly
                var roleClaimType = jwtToken.Claims.FirstOrDefault(c => c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Type ?? "role";
                
                var identity = new ClaimsIdentity(jwtToken.Claims, "jwt", "name", roleClaimType);
                var user = new ClaimsPrincipal(identity);

                return new AuthenticationState(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Auth] Error parsing token: {ex.Message}");
                // If anything fails (e.g. malformed token, secure storage crash), return an anonymous state so Blazor doesn't crash on startup
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public void NotifyAuthenticationStateChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}
