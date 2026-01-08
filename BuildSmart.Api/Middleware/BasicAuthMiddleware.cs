using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Primitives; // Needed for StringValues

namespace BuildSmart.Api.Middleware;

public class BasicAuthMiddleware
{
	private readonly RequestDelegate _next;
	private readonly string _username;
	private readonly string _password;

	public BasicAuthMiddleware(RequestDelegate next, string username, string password)
	{
		_next = next;
		_username = username;
		_password = password;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		if (context.Request.Headers.ContainsKey("Authorization"))
		{
			StringValues authHeaderValues = context.Request.Headers["Authorization"];
			string? authHeader = authHeaderValues.FirstOrDefault(); // Get the first header value

			if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
			{
				var authHeaderValue = AuthenticationHeaderValue.Parse(authHeader);

				if (authHeaderValue.Parameter is not null)
				{
					var credentialBytes = Convert.FromBase64String(authHeaderValue.Parameter);
					var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
					var username = credentials[0];
					var password = credentials[1];

					if (username == _username && password == _password)
					{
						await _next(context);
						return;
					}
				}
			}
			// If it's not a Basic header, or if Basic authentication fails,
			// pass the request to the next middleware (e.g., JWT authentication)
			// This ensures that JWT tokens are not incorrectly processed by BasicAuthMiddleware
			await _next(context);
			return;
		}

		// If no Authorization header is present, pass to next middleware
		await _next(context);
	}
}