using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Linq;

namespace BuildSmart.Api.Tests;

public class TestAuthHandlerOptions : AuthenticationSchemeOptions
{
    public ClaimsPrincipal DefaultUser { get; set; }
}

public class TestAuthHandler : AuthenticationHandler<TestAuthHandlerOptions>
{
    public const string SchemeName = "TestScheme";

    public TestAuthHandler(IOptionsMonitor<TestAuthHandlerOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return AuthenticateResult.NoResult();
        }

        string authorizationHeader = Request.Headers["Authorization"]!;

        if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        string token = authorizationHeader.Substring("Bearer ".Length).Trim();

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            if (jsonToken == null)
            {
                return AuthenticateResult.Fail("Invalid JWT Token");
            }

            var claims = jsonToken.Claims.ToList();
            
            // Ensure we have both long and short forms for role check robustness
            var roleClaims = claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").ToList();
            foreach(var rc in roleClaims)
            {
                if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value == rc.Value))
                    claims.Add(new Claim(ClaimTypes.Role, rc.Value));
            }

            var nameIdClaims = claims.Where(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid" || c.Type == "sub").ToList();
            foreach(var nc in nameIdClaims)
            {
                if (!claims.Any(c => c.Type == ClaimTypes.NameIdentifier && c.Value == nc.Value))
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, nc.Value));
            }

            var identity = new ClaimsIdentity(claims, SchemeName, ClaimTypes.Name, ClaimTypes.Role);
            var principal = new ClaimsPrincipal(identity);
            
            // CRITICAL: Explicitly set the HttpContext User
            Context.User = principal;
            
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        { 
            return AuthenticateResult.Fail(ex.ToString());
        }
    }
}
