using Microsoft.AspNetCore.SignalR.Client;
using BuildSmart.Maui; // Correct namespace for ApiConfig

namespace BuildSmart.Maui.Services;

public class SignalRService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly IAuthService _authService;

    public event Action<string, string, object?>? NotificationReceived;

    public SignalRService(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task ConnectAsync()
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected) return;

        var token = await _authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token)) return;

        var baseUrl = ApiConfig.GetBaseUrl(); // Helper to get "https://localhost:7212" or similar
        // Ensure no trailing slash issues
        var hubUrl = $"{baseUrl.TrimEnd('/')}/hubs/notifications";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string, string, object?>("ReceiveNotification", (title, message, data) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                NotificationReceived?.Invoke(title, message, data);
                
                // Show a global toast/snackbar if possible, or just alert for now
                // Ideally use CommunityToolkit.Maui.Alerts
                var result = await Shell.Current.DisplayAlert(title, message, "View", "OK");
                if (result && data != null)
                {
                    await HandleDeepLinkAsync(data);
                }
            });
        });

        try
        {
            await _hubConnection.StartAsync();
            Console.WriteLine($"SignalR Connected to {hubUrl}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR Connection Error: {ex.Message}");
        }
    }

    private async Task HandleDeepLinkAsync(object data)
    {
        try
        {
            // Use System.Text.Json to parse the data if it comes in as a JsonElement
            if (data is System.Text.Json.JsonElement element)
            {
                if (element.TryGetProperty("route", out var routeProp))
                {
                    var route = routeProp.GetString();
                    if (route == "AuctionHub" && element.TryGetProperty("jobId", out var jobIdProp))
                    {
                        var jobId = jobIdProp.GetString();
                        // Navigate to Auction Hub with JobId
                        await Shell.Current.GoToAsync($"AuctionHubPage?jobId={jobId}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Deep Link Error: {ex.Message}");
        }
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
