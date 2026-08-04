using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace BuildSmart.Maui.Services
{
	

		public class AuthService : BuildSmart.SharedUI.Services.IAuthService
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

		public async Task<string?> RenewTokenAsync(string? currentToken = null)
		{
			var tokenToRenew = currentToken ?? _cachedToken ?? await GetTokenAsync();
			if (string.IsNullOrEmpty(tokenToRenew)) return null;

			try
			{
				using var handler = new System.Net.Http.HttpClientHandler
				{
					ServerCertificateCustomValidationCallback = System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
				};
				using var client = new System.Net.Http.HttpClient(handler);

				var requestUrl = $"{BuildSmart.SharedUI.ApiConfig.GetBaseUrl()}/api/token/renew";
				var content = new System.Net.Http.StringContent(
					System.Text.Json.JsonSerializer.Serialize(new { token = tokenToRenew }),
					System.Text.Encoding.UTF8,
					"application/json");

				var response = await client.PostAsync(requestUrl, content);
				if (response.IsSuccessStatusCode)
				{
					var json = await response.Content.ReadAsStringAsync();
					using var doc = System.Text.Json.JsonDocument.Parse(json);
					if (doc.RootElement.TryGetProperty("token", out var newTokenProp))
					{
						var newToken = newTokenProp.GetString();
						if (!string.IsNullOrEmpty(newToken))
						{
							await SaveTokenAsync(newToken);
							return newToken;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[AuthService] Token renewal error: {ex.Message}");
			}
			return null;
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

		public async Task<string?> AuthenticateWithGoogleAsync()
		{
            var authResult = await Microsoft.Maui.Authentication.WebAuthenticator.Default.AuthenticateAsync(
                new Uri($"{BuildSmart.SharedUI.ApiConfig.GetBaseUrl()}/api/externalauth/google-login?returnUrl=buildsmart://auth"),
                new Uri("buildsmart://"));

            if (authResult?.Properties != null && authResult.Properties.TryGetValue("token", out var token))
            {
                return token;
            }
            return null;
		}

		public async Task<string?> AuthenticateWithAppleAsync()
		{
            var authResult = await Microsoft.Maui.Authentication.WebAuthenticator.Default.AuthenticateAsync(
                new Uri($"{BuildSmart.SharedUI.ApiConfig.GetBaseUrl()}/api/externalauth/apple-login?returnUrl=buildsmart://auth"),
                new Uri("buildsmart://"));

            if (authResult?.Properties != null && authResult.Properties.TryGetValue("token", out var token))
            {
                return token;
            }
            return null;
		}
	}
}

