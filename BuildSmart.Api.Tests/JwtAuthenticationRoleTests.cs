using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace BuildSmart.Api.Tests;

public class JwtAuthenticationRoleTests
{
    private readonly IConfiguration _configuration;

    public JwtAuthenticationRoleTests()
    {
        var inMemorySettings = new System.Collections.Generic.Dictionary<string, string>
        {
            {"Jwt:Key", "SuperSecretKeyForTestingThatIsLongEnough1234567890!"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();
    }

    [Fact]
    public void Test_Jwt_Token_Role_Claim_Resolution_With_Role_String()
    {
        var userId = Guid.NewGuid();
        var tokenString = TestTokenHelper.GenerateJwtToken(userId, "tradesman@test.com", "Tradesman", _configuration);

        // Validate using JsonWebTokenHandler with RoleClaimType = "role" and NameClaimType = "nameid"
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"]!,
            ValidAudience = _configuration["Jwt:Audience"]!,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)),
            RoleClaimType = "role",
            NameClaimType = "nameid"
        };

        var jsonHandler = new JsonWebTokenHandler();
        var result = jsonHandler.ValidateToken(tokenString, tokenValidationParameters);
        Assert.True(result.IsValid);

        var principal = new ClaimsPrincipal(result.ClaimsIdentity);

        Assert.True(principal.IsInRole("Tradesman"));
        Assert.Equal("Tradesman", principal.FindFirst(principal.Identities.First().RoleClaimType)?.Value);
        Assert.Equal(userId.ToString(), principal.Identity?.Name);
    }
}
