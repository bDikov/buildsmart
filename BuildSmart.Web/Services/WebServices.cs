using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.MauiMocks;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace BuildSmart.Web.Services;

public static class BlazorCircuitContext
{
    public static readonly AsyncLocal<IServiceProvider?> CurrentServices = new();
    public static readonly AsyncLocal<string?> CurrentToken = new();
}

public class CircuitContextHandler : CircuitHandler
{
    private readonly IServiceProvider _services;

    public CircuitContextHandler(IServiceProvider services)
    {
        _services = services;
    }

    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next)
    {
        return async context =>
        {
            BlazorCircuitContext.CurrentServices.Value = _services;
            await next(context);
        };
    }
}

public class WebAuthService : IAuthService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;
    private readonly NavigationManager _navigationManager;
    private string? _cachedToken;

    public WebAuthService(IJSRuntime jsRuntime, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor, NavigationManager navigationManager)
    {
        _jsRuntime = jsRuntime;
        _httpContextAccessor = httpContextAccessor;
        _navigationManager = navigationManager;
        
        // EAGERLY capture the token from the HttpContext immediately upon scoped creation (during Prerendering or initial HTTP request)
        try
        {
            var cookieToken = _httpContextAccessor.HttpContext?.Request.Cookies["auth_token"];
            if (!string.IsNullOrEmpty(cookieToken))
            {
                _cachedToken = cookieToken;
                BlazorCircuitContext.CurrentToken.Value = cookieToken;
            }
        }
        catch { }
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_cachedToken);

    public async Task<string?> GetTokenAsync()
    {
        if (_cachedToken != null) return _cachedToken;

        // Try reading from localStorage, then fallback to sessionStorage (if guest)
        try 
        {
            _cachedToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "auth_token");
            if (string.IsNullOrEmpty(_cachedToken))
            {
                _cachedToken = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "auth_token");
            }
        }
        catch (InvalidOperationException) { /* Static rendering context */ }
        catch (JSException) { /* JS not ready */ }
        catch (Exception) { /* Fallback */ }
        return _cachedToken;
    }

    public async Task SaveTokenAsync(string token)
    {
        _cachedToken = token;
        BlazorCircuitContext.CurrentToken.Value = token;
        
        bool isGuest = false;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var email = System.Linq.Enumerable.FirstOrDefault(jwtToken.Claims, c => 
                c.Type == "email" || 
                c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
            isGuest = email != null && email.EndsWith("@buildsmart.guest", StringComparison.OrdinalIgnoreCase);
        }
        catch { }

        try 
        {
            if (isGuest)
            {
                // Save to sessionStorage and set a session cookie (days = null)
                await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "auth_token", token);
                await _jsRuntime.InvokeVoidAsync("setCookie", "auth_token", token, null);
            }
            else
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "auth_token", token);
                await _jsRuntime.InvokeVoidAsync("setCookie", "auth_token", token, 365);
            }
        }
        catch { /* Ignore prerendering errors */ }
    }

    public async Task ClearTokenAsync()
    {
        _cachedToken = null;
        BlazorCircuitContext.CurrentToken.Value = null;
        try 
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_token");
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "auth_token");
            await _jsRuntime.InvokeVoidAsync("setCookie", "auth_token", "", -1);
        }
        catch { /* Ignore prerendering errors */ }
    }

    public string? GetUserRoleFromToken(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        return jwtToken.Claims.FirstOrDefault(c =>
            c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" ||
            c.Type == "role")?.Value;
    }

    public Guid? GetUserIdFromToken(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var userIdStr = jwtToken.Claims.FirstOrDefault(c =>
            c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/nameidentifier" ||
            c.Type == "nameid" ||
            c.Type == "sub")?.Value;
        if (Guid.TryParse(userIdStr, out var userId)) return userId;
        return null;
    }

    public Task<string?> AuthenticateWithGoogleAsync()
    {
        // Navigate the whole window to the API external auth endpoint. 
        // We set returnUrl to the absolute URL of the Blazor Web App so the API redirects back here instead of to itself.
        var returnUrl = _navigationManager.ToAbsoluteUri("/login").ToString();
        _navigationManager.NavigateTo($"{BuildSmart.SharedUI.ApiConfig.GetBaseUrl()}/api/externalauth/google-login?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
        return Task.FromResult<string?>(null);
    }

    public Task<string?> AuthenticateWithAppleAsync()
    {
        var returnUrl = _navigationManager.ToAbsoluteUri("/login").ToString();
        _navigationManager.NavigateTo($"{BuildSmart.SharedUI.ApiConfig.GetBaseUrl()}/api/externalauth/apple-login?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
        return Task.FromResult<string?>(null);
    }
}

public class WebAlertService : IAlertService
{
    private readonly IJSRuntime _jsRuntime;

    public WebAlertService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task DisplayAlert(string title, string message, string cancel)
    {
        if (AppServiceLocator.ToastAction != null)
        {
            var type = "info";
            var titleLower = title?.ToLower() ?? "";
            if (titleLower.Contains("success") || titleLower.Contains("успех")) type = "success";
            else if (titleLower.Contains("error") || titleLower.Contains("грешка") || titleLower.Contains("limit") || titleLower.Contains("required") || titleLower.Contains("лимит")) type = "error";

            await AppServiceLocator.ToastAction(message, type);
            return;
        }

        try { await _jsRuntime.InvokeVoidAsync("alert", $"{title}\n{message}"); } catch { }
    }

    public async Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
    {
        try { return await _jsRuntime.InvokeAsync<bool>("confirm", $"{title}\n{message}"); } catch { return false; }
    }

    public async Task<string> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string placeholder = null, int maxLength = -1, object keyboard = null, string initialValue = "")
    {
        try { return await _jsRuntime.InvokeAsync<string>("prompt", $"{title}\n{message}", initialValue); } catch { return string.Empty; }
    }
}

public class WebNavigationBridge : INavigationBridge
{
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jsRuntime;

    public WebNavigationBridge(NavigationManager navigationManager, IJSRuntime jsRuntime)
    {
        _navigationManager = navigationManager;
        _jsRuntime = jsRuntime;
    }

    public Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        var url = route;

        if (url == "..")
        {
            var path = new Uri(_navigationManager.Uri).AbsolutePath.ToLower();
            if (path.Contains("/category-detail"))
            {
                _navigationManager.NavigateTo("/category-management");
                return Task.CompletedTask;
            }
            if (path.Contains("/user-edit"))
            {
                _navigationManager.NavigateTo("/user-management");
                return Task.CompletedTask;
            }
            
            try { _jsRuntime.InvokeVoidAsync("history.back"); } catch { }
            return Task.CompletedTask;
        }

        if (url == "../..")
        {
            try { _jsRuntime.InvokeVoidAsync("history.go", -2); } catch { }
            return Task.CompletedTask;
        }

        // Parse query string if present inside url to perform EndsWith("Page") check correctly
        string pathPart = url;
        string queryPart = "";
        int qIndex = url.IndexOf('?');
        if (qIndex >= 0)
        {
            pathPart = url.Substring(0, qIndex);
            queryPart = url.Substring(qIndex);
        }

        // Map old Page-based routes to new kebab-case routes for backward compatibility
        if (pathPart.EndsWith("Page"))
        {
            var pageName = pathPart.Replace("Page", "");
            var mappedPath = pageName switch
            {
                "CreateAccount" => "/create-account",
                "JobWizard" => "/job-wizard",
                "ProjectDetail" => "/project-detail",
                "PassedAuctions" => "/passed-auctions",
                "Notifications" => "/notifications",
                "ActiveJobs" => "/active-jobs",
                "TradesmanDetails" => "/tradesman-details",
                "AuctionHub" => "/auction-hub",
                "ScopeReview" => "/scope-review",
                "TaskBreakdown" => "/task-breakdown",
                "BidDetails" => "/bid-details",
                "PlaceBid" => "/place-bid",
                "TradesmanBookingDashboard" => "/tradesman-booking-dashboard",
                "Checkout" => "/checkout",
                "BookingDashboard" => "/booking-dashboard",
                "LoginPage" => "/login",
                "BlazorHost" => "/",
                "CategoryManagement" => "/category-management",
                "CategoryDetail" => "/category-detail",
                "AdminCategorySkus" => "/admin-category-skus",
                _ => pathPart
            };
            url = mappedPath + queryPart;
        }

        // Intercept native MAUI shell routes and translate them to Web routes
        if (url == "//BlazorHostPage") url = "/";
        else if (url == "//LoginPage") url = "/login";
        else if (url.StartsWith("//")) url = url.Substring(2);
        
        // Ensure absolute path if it doesn't look like a full URL or already starting with /
        if (!url.StartsWith("/") && !url.Contains("://") && url != "..")
        {
            url = "/" + url;
        }

        if (parameters != null && parameters.Count > 0)
        {
            var separator = url.Contains("?") ? "&" : "?";
            var queryString = string.Join("&", parameters.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value?.ToString() ?? string.Empty)}"));
            url = $"{url}{separator}{queryString}";
        }

        // Only force load the root to reset app state. Force loading /login clears JS state during logout.
        bool forceLoad = url == "/";
        _navigationManager.NavigateTo(url, forceLoad);
        return Task.CompletedTask;
    }

    public Task GoBackAsync()
    {
        try { _jsRuntime.InvokeVoidAsync("history.back"); } catch { }
        return Task.CompletedTask;
    }
}

public class WebAppMainThread : IAppMainThread
{
    public Func<Action, Task>? DispatcherInvoke { get; set; }
    public Func<Func<Task>, Task>? DispatcherInvokeAsync { get; set; }

    public void BeginInvokeOnMainThread(Action action)
    {
        var capturedServices = BlazorCircuitContext.CurrentServices.Value;
        if (DispatcherInvoke != null)
        {
            _ = DispatcherInvoke(() => 
            {
                var original = BlazorCircuitContext.CurrentServices.Value;
                try 
                { 
                    BlazorCircuitContext.CurrentServices.Value = capturedServices;
                    action(); 
                }
                catch (Exception ex) { Console.WriteLine($"Dispatcher error: {ex}"); }
                finally { BlazorCircuitContext.CurrentServices.Value = original; }
            });
        }
        else
        {
            Task.Run(() => 
            {
                var original = BlazorCircuitContext.CurrentServices.Value;
                try 
                { 
                    BlazorCircuitContext.CurrentServices.Value = capturedServices;
                    action(); 
                }
                catch (Exception ex) { Console.WriteLine($"Task.Run error: {ex}"); }
                finally { BlazorCircuitContext.CurrentServices.Value = original; }
            });
        }
    }

    public Task InvokeOnMainThreadAsync(Action action)
    {
        var capturedServices = BlazorCircuitContext.CurrentServices.Value;
        if (DispatcherInvoke != null) 
        {
            return DispatcherInvoke(() => 
            {
                var original = BlazorCircuitContext.CurrentServices.Value;
                try 
                { 
                    BlazorCircuitContext.CurrentServices.Value = capturedServices;
                    action(); 
                }
                catch (Exception ex) { Console.WriteLine($"Dispatcher error: {ex}"); throw; }
                finally { BlazorCircuitContext.CurrentServices.Value = original; }
            });
        }
        action();
        return Task.CompletedTask;
    }

    public Task InvokeOnMainThreadAsync(Func<Task> func)
    {
        var capturedServices = BlazorCircuitContext.CurrentServices.Value;
        if (DispatcherInvokeAsync != null) 
        {
            return DispatcherInvokeAsync(async () => 
            {
                var original = BlazorCircuitContext.CurrentServices.Value;
                try 
                { 
                    BlazorCircuitContext.CurrentServices.Value = capturedServices;
                    await func(); 
                }
                catch (Exception ex) { Console.WriteLine($"Dispatcher error: {ex}"); throw; }
                finally { BlazorCircuitContext.CurrentServices.Value = original; }
            });
        }
        return func();
    }
}

public class WebMediaPicker : IMediaPicker
{
    public Task<FileResult?> PickPhotoAsync(MediaPickerOptions? options = null) => Task.FromResult<FileResult?>(null);
    public Task<FileResult?> PickVideoAsync(MediaPickerOptions? options = null) => Task.FromResult<FileResult?>(null);
}

public class WebFilePicker : IFilePicker
{
    public Task<FileResult?> PickAsync(PickOptions? options = null) => Task.FromResult<FileResult?>(null);
}
