using Microsoft.AspNetCore.SignalR.Client;
using BuildSmart.SharedUI; // Correct namespace for ApiConfig
using BuildSmart.SharedUI.MauiMocks;

namespace BuildSmart.SharedUI.Services;

public class SignalRService : IAsyncDisposable
{
	private HubConnection? _hubConnection;
	private HubConnection? _jobProcessingConnection;
	private readonly IAuthService _authService;

	public event Action<string, string, object?>? NotificationReceived;

	public event Action<System.Text.Json.JsonElement>? QuestionUpdated;

	public event Action<System.Text.Json.JsonElement>? NewReplyReceived;

	public event Action<Guid>? OfferRegenerated;

	public event Action<System.Text.Json.JsonElement>? ProjectMessageReceived;

	public event Action? NotificationsStateChanged;

	public event Action<string, bool>? UserPresenceChanged;

	public event Action<int, string, int>? ProcessingUpdateReceived;

	public void NotifyNotificationsStateChanged()
	{
		NotificationsStateChanged?.Invoke();
	}

	private readonly INavigationBridge _navigation;
	private readonly IAlertService _alerts;
	private readonly IAppMainThread _mainThread;

	public SignalRService(
		IAuthService authService,
		INavigationBridge navigation,
		IAlertService alerts,
		IAppMainThread mainThread)
	{
		_authService = authService;
		_navigation = navigation;
		_alerts = alerts;
		_mainThread = mainThread;
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
#if DEBUG
				options.HttpMessageHandlerFactory = (messageHandler) =>
				{
#pragma warning disable CA1416
					if (!OperatingSystem.IsBrowser() && messageHandler is System.Net.Http.HttpClientHandler clientHandler)
					{
						clientHandler.ServerCertificateCustomValidationCallback =
							System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
					}
#pragma warning restore CA1416
					return messageHandler;
				};
#endif
			})
			.WithAutomaticReconnect()
			.Build();

		_hubConnection.On<string, string, object?>("ReceiveNotification", (title, message, data) =>
		{
			_mainThread.BeginInvokeOnMainThread(async () =>
			{
				NotificationReceived?.Invoke(title, message, data);

				bool shouldShowAlert = true;
				if (data is System.Text.Json.JsonElement element && element.TryGetProperty("route", out var routeProp))
				{
					if (routeProp.GetString() == "ProjectMessages")
					{
						shouldShowAlert = false;
					}
				}

				if (shouldShowAlert)
				{
					var result = await _alerts.DisplayAlert(title, message, "View", "OK");
					if (result && data != null)
					{
						await HandleDeepLinkAsync(data);
					}
				}
			});
		});

		_hubConnection.On<System.Text.Json.JsonElement>("ReceiveQuestionUpdate", (payload) =>
		{
			_mainThread.BeginInvokeOnMainThread(() => QuestionUpdated?.Invoke(payload));
		});

		_hubConnection.On<System.Text.Json.JsonElement>("ReceiveNewReply", (payload) =>
		{
			_mainThread.BeginInvokeOnMainThread(() => NewReplyReceived?.Invoke(payload));
		});

		_hubConnection.On<Guid>("OfferRegenerated", (projectId) =>
		{
			_mainThread.BeginInvokeOnMainThread(() => OfferRegenerated?.Invoke(projectId));
		});

		_hubConnection.On<System.Text.Json.JsonElement>("ReceiveProjectMessage", (payload) =>
		{
			_mainThread.BeginInvokeOnMainThread(() => ProjectMessageReceived?.Invoke(payload));
		});

		_hubConnection.On<string, bool>("UserPresenceChanged", (userId, isOnline) =>
		{
			_mainThread.BeginInvokeOnMainThread(() => UserPresenceChanged?.Invoke(userId, isOnline));
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

	public async Task JoinAuctionGroupAsync(string jobId)
	{
		await ConnectAsync();
		if (_hubConnection?.State == HubConnectionState.Connected)
		{
			await _hubConnection.InvokeAsync("JoinAuctionGroup", jobId);
		}
	}

	public async Task LeaveAuctionGroupAsync(string jobId)
	{
		if (_hubConnection?.State == HubConnectionState.Connected)
		{
			await _hubConnection.InvokeAsync("LeaveAuctionGroup", jobId);
		}
	}

	public async Task JoinProjectGroupAsync(string projectId)
	{
		await ConnectAsync();
		if (_hubConnection?.State == HubConnectionState.Connected)
		{
			await _hubConnection.InvokeAsync("JoinProjectGroup", projectId);
		}
	}

	public async Task LeaveProjectGroupAsync(string projectId)
	{
		if (_hubConnection?.State == HubConnectionState.Connected)
		{
			await _hubConnection.InvokeAsync("LeaveProjectGroup", projectId);
		}
	}

	public async Task JoinSupportGroupAsync()
	{
		await ConnectAsync();
		if (_hubConnection?.State == HubConnectionState.Connected)
		{
			await _hubConnection.InvokeAsync("JoinSupportGroup");
		}
	}

	public async Task LeaveSupportGroupAsync()
	{
		if (_hubConnection?.State == HubConnectionState.Connected)
		{
			await _hubConnection.InvokeAsync("LeaveSupportGroup");
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
						await _navigation.NavigateToAsync($"AuctionHubPage?jobId={jobId}");
					}
					else if (route == "ProjectMessages" && element.TryGetProperty("projectId", out var projectIdProp))
					{
						var projectId = projectIdProp.GetString();
						await _navigation.NavigateToAsync($"/project-messages?projectId={projectId}");
					}
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Deep Link Error: {ex.Message}");
		}
	}

	public async Task ConnectJobProcessingAsync()
	{
		if (_jobProcessingConnection != null && _jobProcessingConnection.State == HubConnectionState.Connected) return;

		var token = await _authService.GetTokenAsync();
		if (string.IsNullOrEmpty(token)) return;

		var baseUrl = ApiConfig.GetBaseUrl();
		var hubUrl = $"{baseUrl.TrimEnd('/')}/jobProcessingHub";

		_jobProcessingConnection = new HubConnectionBuilder()
			.WithUrl(hubUrl, options =>
			{
				options.AccessTokenProvider = () => Task.FromResult<string?>(token);
#if DEBUG
				options.HttpMessageHandlerFactory = (messageHandler) =>
				{
#pragma warning disable CA1416
					if (!OperatingSystem.IsBrowser() && messageHandler is System.Net.Http.HttpClientHandler clientHandler)
					{
						clientHandler.ServerCertificateCustomValidationCallback =
							System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
					}
#pragma warning restore CA1416
					return messageHandler;
				};
#endif
			})
			.WithAutomaticReconnect()
			.Build();

		_jobProcessingConnection.On<int, string, int>("ReceiveProcessingUpdate", (step, message, progress) =>
		{
			_mainThread.BeginInvokeOnMainThread(() =>
			{
				ProcessingUpdateReceived?.Invoke(step, message, progress);
			});
		});

		try
		{
			await _jobProcessingConnection.StartAsync();
			Console.WriteLine($"SignalR Connected to {hubUrl}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"SignalR JobProcessing Connection Error: {ex.Message}");
		}
	}

	public async Task JoinJobProcessingGroupAsync(string projectId)
	{
		await ConnectJobProcessingAsync();
		if (_jobProcessingConnection?.State == HubConnectionState.Connected)
		{
			await _jobProcessingConnection.InvokeAsync("JoinProjectGroup", projectId);
		}
	}

	public async Task LeaveJobProcessingGroupAsync(string projectId)
	{
		if (_jobProcessingConnection?.State == HubConnectionState.Connected)
		{
			await _jobProcessingConnection.InvokeAsync("LeaveProjectGroup", projectId);
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
		if (_jobProcessingConnection != null)
		{
			await _jobProcessingConnection.StopAsync();
			await _jobProcessingConnection.DisposeAsync();
			_jobProcessingConnection = null;
		}
	}

	public async ValueTask DisposeAsync()
	{
		await DisconnectAsync();
	}
}