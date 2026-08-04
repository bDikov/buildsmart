using System.Net.Http.Headers;
using BuildSmart.SharedUI.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.IdentityModel.Tokens.Jwt;

namespace BuildSmart.SharedUI.Handlers;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IAuthService _authService;

    public AuthHeaderHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Avoid intercepting requests to the renew endpoint itself to prevent recursion
        if (request.RequestUri != null && request.RequestUri.AbsolutePath.EndsWith("/api/token/renew", StringComparison.OrdinalIgnoreCase))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        IAuthService currentAuthService = _authService;

        // In Blazor Server, HttpClientFactory creates handlers in the root scope, which isolates them from the user's circuit.
        // We override the root IAuthService with the circuit's scoped service if the execution context flows it down to us.
        var circuitContextType = Type.GetType("BuildSmart.Web.Services.BlazorCircuitContext, BuildSmart.Web");
        if (circuitContextType != null)
        {
            var field = circuitContextType.GetField("CurrentServices");
            if (field != null && field.GetValue(null) is System.Threading.AsyncLocal<IServiceProvider?> asyncLocal)
            {
                var services = asyncLocal.Value;
                if (services != null)
                {
                    try
                    {
                        if (services.GetService(typeof(IAuthService)) is IAuthService scopedService)
                        {
                            currentAuthService = scopedService;
                        }
                    }
                    catch (ObjectDisposedException) { }
                }
            }
        }

        var token = await currentAuthService.GetTokenAsync();

        // Fallback to AsyncLocal token if scoped service failed (e.g. disposed scope on background thread)
        if (string.IsNullOrEmpty(token))
        {
            if (circuitContextType != null)
            {
                var field = circuitContextType.GetField("CurrentToken");
                if (field != null && field.GetValue(null) is System.Threading.AsyncLocal<string?> asyncLocalToken)
                {
                    token = asyncLocalToken.Value;
                }
            }
        }

        // Proactive Renewal: If user interacts and token is expiring in < 5 mins (or expired), auto-renew for another 30 mins
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var jwtHandler = new JwtSecurityTokenHandler();
                if (jwtHandler.CanReadToken(token))
                {
                    var jwtToken = jwtHandler.ReadJwtToken(token);
                    if (jwtToken.ValidTo <= DateTime.UtcNow.AddMinutes(5))
                    {
                        Console.WriteLine("[AuthHeaderHandler] Active user interaction detected & token expiring soon. Auto-renewing...");
                        var renewedToken = await currentAuthService.RenewTokenAsync(token);
                        if (!string.IsNullOrEmpty(renewedToken))
                        {
                            token = renewedToken;
                            Console.WriteLine("[AuthHeaderHandler] Token successfully auto-renewed for another 30 minutes!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthHeaderHandler] Exception during token check: {ex.Message}");
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            Console.WriteLine("[AuthHeaderHandler] WARNING: Token is NULL or EMPTY!");
        }

        // Set Accept-Language header based on current UI culture
        try
        {
            var currentCulture = System.Globalization.CultureInfo.CurrentUICulture ?? System.Globalization.CultureInfo.CurrentCulture;
            if (currentCulture != null)
            {
                request.Headers.AcceptLanguage.Clear();
                request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(currentCulture.Name));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthHeaderHandler] Failed to set Accept-Language header: {ex.Message}");
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Reactive Renewal: If request returned 401 Unauthorized, attempt token renewal
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(token))
        {
            Console.WriteLine("[AuthHeaderHandler] HTTP 401 Unauthorized received. Attempting reactive token renewal...");
            var renewedToken = await currentAuthService.RenewTokenAsync(token);
            if (!string.IsNullOrEmpty(renewedToken))
            {
                Console.WriteLine("[AuthHeaderHandler] Reactive token renewal succeeded.");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", renewedToken);
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[AuthHeaderHandler] HTTP Request failed with status code: {response.StatusCode}");
        }
        return response;
    }
}

