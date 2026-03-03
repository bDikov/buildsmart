using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace BuildSmart.Maui.Services
{
	public interface IAuthService
	{
		Task<string?> GetTokenAsync();

		Task SaveTokenAsync(string token);

		Task ClearTokenAsync();

		bool IsAuthenticated { get; }

		string? GetUserRoleFromToken(string? token);

		Guid? GetUserIdFromToken(string? token);
		}

		public class AuthService : IAuthService
		{
		private const string TokenKey = "auth_token";
		private string? _cachedToken;

		public bool IsAuthenticated => !string.IsNullOrEmpty(_cachedToken);

		public async Task<string?> GetTokenAsync()
		{
			if (_cachedToken != null) return _cachedToken;

			_cachedToken = await SecureStorage.Default.GetAsync(TokenKey);
			return _cachedToken;
		}

		public async Task SaveTokenAsync(string token)
		{
			_cachedToken = token;
			await SecureStorage.Default.SetAsync(TokenKey, token);
		}

		public async Task ClearTokenAsync()
		{
			_cachedToken = null;
			SecureStorage.Default.Remove(TokenKey);
			await Task.CompletedTask;
		}

		public string? GetUserRoleFromToken(string? token)
		{
			if (string.IsNullOrEmpty(token))
			{
				return null;
			}

			var handler = new JwtSecurityTokenHandler();
			var jwtToken = handler.ReadJwtToken(token);

			// Option 1: Check for the short name "role" if the long one fails
			return jwtToken.Claims.FirstOrDefault(c =>
				c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" ||
				c.Type == "role")?.Value;
		}

		public Guid? GetUserIdFromToken(string? token)
		{
			if (string.IsNullOrEmpty(token))
			{
				return null;
			}

			var handler = new JwtSecurityTokenHandler();
			var jwtToken = handler.ReadJwtToken(token);

			var userIdStr = jwtToken.Claims.FirstOrDefault(c =>
				c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/nameidentifier" ||
				c.Type == "nameid" ||
				c.Type == "sub")?.Value;

			if (Guid.TryParse(userIdStr, out var userId))
			{
				return userId;
			}

			return null;
		}
		}
		}