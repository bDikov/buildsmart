using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using System;

namespace BuildSmart.Api.Services;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly string _secretToken;

    public HangfireDashboardAuthorizationFilter(string secretToken)
    {
        _secretToken = secretToken;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Allow localhost connections unconditionally
        var ip = httpContext.Connection.RemoteIpAddress;
        if (ip != null)
        {
            if (System.Net.IPAddress.IsLoopback(ip))
            {
                return true;
            }
        }

        // If the secret token is not configured in the environment, reject all remote requests
        if (string.IsNullOrWhiteSpace(_secretToken))
        {
            return false;
        }

        // Check if token is passed via query string
        if (httpContext.Request.Query.TryGetValue("token", out var tokenValue) && tokenValue == _secretToken)
        {
            // Drop a cookie to maintain dashboard access
            httpContext.Response.Cookies.Append("hangfire_dashboard_token", _secretToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
            
            // Instantly redirect to the clean path (without the query string) to strip the token from history and referrer headers
            httpContext.Response.Redirect(httpContext.Request.Path);
            return true;
        }

        // Check if valid token cookie is present
        if (httpContext.Request.Cookies.TryGetValue("hangfire_dashboard_token", out var cookieValue) && cookieValue == _secretToken)
        {
            return true;
        }

        return false;
    }
}
