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
}