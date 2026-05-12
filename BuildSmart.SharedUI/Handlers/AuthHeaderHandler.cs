using System.Net.Http.Headers;
using BuildSmart.SharedUI.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

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

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            Console.WriteLine("[AuthHeaderHandler] WARNING: Token is NULL or EMPTY!");
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[AuthHeaderHandler] HTTP Request failed with status code: {response.StatusCode}");
        }
        return response;
    }
}

