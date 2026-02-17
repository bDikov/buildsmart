using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

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
        if (Options.DefaultUser != null)
        {
            var ticket = new AuthenticationTicket(Options.DefaultUser, SchemeName);
            return AuthenticateResult.Success(ticket);
        }

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
            var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

            if (jsonToken == null)
            {
                return AuthenticateResult.Fail("Invalid JWT Token");
            }

            // For testing purposes, we'll just create a ClaimsPrincipal from the token's claims
            // In a real scenario, you would also validate the token's signature, issuer, audience, etc.
            var identity = new ClaimsIdentity(jsonToken.Claims, SchemeName, ClaimTypes.Name, ClaimTypes.Role);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        { 
            return AuthenticateResult.Fail(ex.ToString());
        }
    }
}