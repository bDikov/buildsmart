using BuildSmart.Api.DTOs;
using BuildSmart.Core.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BuildSmart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TokenController : ControllerBase
{
	private readonly IUnitOfWork _unitOfWork;
	private readonly IConfiguration _configuration;

	public TokenController(IUnitOfWork unitOfWork, IConfiguration configuration)
	{
		_unitOfWork = unitOfWork;
		_configuration = configuration;
	}

	[HttpPost]
	public async Task<IActionResult> CreateToken([FromBody] LoginRequest loginRequest)
	{
		var user = await _unitOfWork.Users.GetByEmailAsync(loginRequest.Email);

		if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.HashedPassword))
		{
			return Unauthorized();
		}

		var issuer = _configuration["Jwt:Issuer"];
		var audience = _configuration["Jwt:Audience"];
		var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);

		var tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(new[]
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(ClaimTypes.Role, user.Role.ToString())
			}),
			Expires = DateTime.UtcNow.AddMinutes(30),
			Issuer = issuer,
			Audience = audience,
			SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
		};

		var tokenHandler = new JwtSecurityTokenHandler();
		var token = tokenHandler.CreateToken(tokenDescriptor);
		var jwtToken = tokenHandler.WriteToken(token);

		return Ok(jwtToken);
	}

	[HttpPost("renew")]
	public async Task<IActionResult> RenewToken([FromBody] RenewTokenRequest request)
	{
		if (string.IsNullOrEmpty(request.Token))
		{
			return BadRequest(new { message = "Token is required" });
		}

		try
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]!);

			var validationParameters = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(key),
				ValidateIssuer = true,
				ValidIssuer = _configuration["Jwt:Issuer"],
				ValidateAudience = true,
				ValidAudience = _configuration["Jwt:Audience"],
				ValidateLifetime = false, // Allow token renewal within sliding grace window
				ClockSkew = TimeSpan.Zero
			};

			var principal = tokenHandler.ValidateToken(request.Token, validationParameters, out var validatedToken);

			var userIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (!Guid.TryParse(userIdStr, out var userId))
			{
				return Unauthorized(new { message = "Invalid token claims" });
			}

			var user = await _unitOfWork.Users.GetByIdAsync(userId);
			if (user == null)
			{
				return Unauthorized(new { message = "User no longer exists" });
			}

			var newTokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new[]
				{
					new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
					new Claim(ClaimTypes.Email, user.Email),
					new Claim(ClaimTypes.Role, user.Role.ToString())
				}),
				Expires = DateTime.UtcNow.AddMinutes(30),
				Issuer = _configuration["Jwt:Issuer"],
				Audience = _configuration["Jwt:Audience"],
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};

			var newToken = tokenHandler.CreateToken(newTokenDescriptor);
			var jwtToken = tokenHandler.WriteToken(newToken);

			return Ok(new { token = jwtToken });
		}
		catch (Exception ex)
		{
			return Unauthorized(new { message = $"Token renewal failed: {ex.Message}" });
		}
	}
}