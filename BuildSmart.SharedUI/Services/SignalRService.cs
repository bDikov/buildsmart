using Microsoft.AspNetCore.SignalR.Client;
using BuildSmart.SharedUI; // Correct namespace for ApiConfig
using BuildSmart.SharedUI.MauiMocks;
using System.Threading;

namespace BuildSmart.SharedUI.Services;

public class SignalRService : IAsyncDisposable
{
	private HubConnection? _hubConnection;
	private HubConnection? _jobProcessingConnection;
	private readonly IAuthService _authService;
	private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
	private readonly SemaphoreSlim _jobConnectionSemaphore = new(1, 1);

	public event Action<string, string, object?>? NotificationReceived;

	public event Action<System.Text.Json.JsonElement>? QuestionUpdated;

	public event Action<System.Text.Json.JsonElement>? NewReplyReceived;

	public event Action<Guid>? OfferRegenerated;

	public event Action<System.Text.Json.JsonElement>? ProjectMessageReceived;

	public event Action? NotificationsStateChanged;

	public event Action<string, bool>? UserPresenceChanged;

	public event Action<int, string, int>? ProcessingUpdateReceived;

	public event Action<string, string, string>? LocalizationUpdated;

	public void NotifyNotificationsStateChanged()
	{
		NotificationsStateChanged?.Invoke();
	}

	private readonly INavigationBridge _navigation;
	private readonly IAlertService _alerts;
	private readonly IAppMainThread _mainThread;
	private readonly BuildSmart.Core.Application.Interfaces.ILocalizationCacheService? _cacheService;
	private readonly ILocalizationStateService? _stateService;

	public SignalRService(
		IAuthService authService,
		INavigationBridge navigation,
		IAlertService alerts,
		IAppMainThread mainThread,
		BuildSmart.Core.Application.Interfaces.ILocalizationCacheService? cacheService = null,
		ILocalizationStateService? stateService = null)
	{
		_authService = authService;
		_navigation = navigation;
		_alerts = alerts;
		_mainThread = mainThread;
		_cacheService = cacheService;
		_stateService = stateService;
	}

	public async Task ConnectAsync()
	{
		await _connectionSemaphore.WaitAsync();
		try
		{
			if (_hubConnection != null)
			{
				if (_hubConnection.State == HubConnectionState.Connected ||
					_hubConnection.State == HubConnectionState.Connecting ||
					_hubConnection.State == HubConnectionState.Reconnecting)
				{
					return;
				}

				try
				{
					await _hubConnection.StartAsync();
					return;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"SignalR Connection restart failed, rebuilding connection: {ex.Message}");
					try
					{
						await _hubConnection.StopAsync();
						await _hubConnection.DisposeAsync();
					}
					catch { }
					_hubConnection = null;
				}
			}

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

					bool isOfferReady = string.Equals(title, "Offer Ready", StringComparison.OrdinalIgnoreCase) || 
					                    string.Equals(title, "Офертата е Готова", StringComparison.OrdinalIgnoreCase);

					if (isOfferReady && AppServiceLocator.ToastAction != null)
					{
						string? downloadUrl = null;
						if (data is System.Text.Json.JsonElement docElement && docElement.TryGetProperty("id", out var idProp))
						{
							var projectId = idProp.GetString();
							downloadUrl = $"{ApiConfig.GetBaseUrl().TrimEnd('/')}/api/offers/{projectId}/download";
						}
						
						await AppServiceLocator.ToastAction(message, "success", downloadUrl);
					}
					else
					{
						if (shouldShowAlert)
						{
							var result = await _alerts.DisplayAlert(title, message, "View", "OK");
							if (result && data != null)
							{
								await HandleDeepLinkAsync(data);
							}
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

			_hubConnection.On<string, string, string>("ReceiveLocalizationUpdate", (key, culture, newValue) =>
			{
				_mainThread.BeginInvokeOnMainThread(() =>
				{
					_cacheService?.Set(key, culture, newValue);
					LocalizationUpdated?.Invoke(key, culture, newValue);
					_stateService?.NotifyLocalizationChanged();
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
		finally
		{
			_connectionSemaphore.Release();
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
						await _navigation.NavigateToAsync($"/project-detail?jobId={jobId}");
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
		await _jobConnectionSemaphore.WaitAsync();
		try
		{
			if (_jobProcessingConnection != null)
			{
				if (_jobProcessingConnection.State == HubConnectionState.Connected ||
					_jobProcessingConnection.State == HubConnectionState.Connecting ||
					_jobProcessingConnection.State == HubConnectionState.Reconnecting)
				{
					return;
				}

				try
				{
					await _jobProcessingConnection.StartAsync();
					return;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"SignalR JobProcessing restart failed, rebuilding connection: {ex.Message}");
					try
					{
						await _jobProcessingConnection.StopAsync();
						await _jobProcessingConnection.DisposeAsync();
					}
					catch { }
					_jobProcessingConnection = null;
				}
			}

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
		finally
		{
			_jobConnectionSemaphore.Release();
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