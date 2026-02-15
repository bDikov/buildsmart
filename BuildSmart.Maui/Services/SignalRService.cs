using Microsoft.AspNetCore.SignalR.Client;
using BuildSmart.Maui; // Correct namespace for ApiConfig

namespace BuildSmart.Maui.Services;

public class SignalRService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly IAuthService _authService;

    public event Action<string, string>? NotificationReceived;

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

        _hubConnection.On<string, string>("ReceiveNotification", (title, message) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                NotificationReceived?.Invoke(title, message);
                
                // Show a global toast/snackbar if possible, or just alert for now
                // Ideally use CommunityToolkit.Maui.Alerts
                await Shell.Current.DisplayAlert(title, message, "OK");
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
