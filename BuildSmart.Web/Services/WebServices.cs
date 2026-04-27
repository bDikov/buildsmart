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
    private string? _cachedToken;

    public WebAuthService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_cachedToken);

    public async Task<string?> GetTokenAsync()
    {
        if (_cachedToken != null) return _cachedToken;
        try 
        {
            // Only execute if not prerendering
            _cachedToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "auth_token");
        }
        catch (InvalidOperationException) { /* Static rendering context */ }
        catch (JSException) { /* JS not ready */ }
        catch (Exception) { /* Fallback */ }
        return _cachedToken;
    }

    public async Task SaveTokenAsync(string token)
    {
        _cachedToken = token;
        try 
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "auth_token", token);
        }
        catch { /* Ignore prerendering errors */ }
    }

    public async Task ClearTokenAsync()
    {
        _cachedToken = null;
        try 
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_token");
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
        
        // Intercept native MAUI shell routes and translate them to Web routes
        if (url == "//BlazorHostPage") url = "/";
        else if (url == "//LoginPage") url = "/login";
        else if (url.StartsWith("//")) url = url.Substring(2);
        
        if (parameters != null && parameters.Count > 0)
        {
            var queryString = string.Join("&", parameters.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value?.ToString() ?? string.Empty)}"));
            url = $"{url}?{queryString}";
        }

        _navigationManager.NavigateTo(url);
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
        if (DispatcherInvoke != null)
        {
            _ = DispatcherInvoke(() => 
            {
                try { action(); }
                catch (Exception ex) { Console.WriteLine($"Dispatcher error: {ex}"); }
            });
        }
        else
        {
            Task.Run(() => 
            {
                try { action(); }
                catch (Exception ex) { Console.WriteLine($"Task.Run error: {ex}"); }
            });
        }
    }

    public Task InvokeOnMainThreadAsync(Action action)
    {
        if (DispatcherInvoke != null) 
        {
            return DispatcherInvoke(() => 
            {
                try { action(); }
                catch (Exception ex) { Console.WriteLine($"Dispatcher error: {ex}"); throw; }
            });
        }
        action();
        return Task.CompletedTask;
    }

    public Task InvokeOnMainThreadAsync(Func<Task> func)
    {
        if (DispatcherInvokeAsync != null) 
        {
            return DispatcherInvokeAsync(async () => 
            {
                try { await func(); }
                catch (Exception ex) { Console.WriteLine($"Dispatcher error: {ex}"); throw; }
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
