using BuildSmart.Maui.GraphQL;
using BuildSmart.Maui.Views;
using BuildSmart.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuildSmart.Maui.ViewModels
{
	public partial class LoginPageViewModel : ObservableObject
	{
		private readonly IBuildSmartApiClient _apiClient;
		private readonly IServiceProvider _serviceProvider;
		private readonly IAuthService _authService;

		[ObservableProperty]
		private string _email = string.Empty;

		[ObservableProperty]
		private string _password = string.Empty;

		public LoginPageViewModel(IBuildSmartApiClient apiClient, IServiceProvider serviceProvider, IAuthService authService)
		{
			_apiClient = apiClient;
			_serviceProvider = serviceProvider;
			_authService = authService;
		}

		[RelayCommand]
		private async Task LoginAsync()
		{
			try
			{
				// Clear any existing token before logging in to ensure a clean state
				await _authService.ClearTokenAsync();

				using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
				var result = await _apiClient.Login.ExecuteAsync(Email, Password, cts.Token);

				if (result.Errors.Count > 0)
				{
					var errorMessage = result.Errors.FirstOrDefault()?.Message ?? "Unknown GraphQL error.";
					await MainThread.InvokeOnMainThreadAsync(() =>
						Shell.Current.DisplayAlert("Login Failed", errorMessage, "OK"));
					return;
				}

				if (!string.IsNullOrEmpty(result.Data?.Login))

				{
					var token = result.Data.Login;

					await _authService.SaveTokenAsync(token);

					var userRole = _authService.GetUserRoleFromToken(token);

					await MainThread.InvokeOnMainThreadAsync(() =>

					{
						if (userRole == "Admin")

						{
							Application.Current.MainPage = _serviceProvider.GetRequiredService<AdminShell>();
						}
						else

						{
							Application.Current.MainPage = _serviceProvider.GetRequiredService<MainShell>();
						}
					});
				}
				else
				{
					await MainThread.InvokeOnMainThreadAsync(() =>
						Shell.Current.DisplayAlert("Login Failed", "Received an empty or invalid response from the server.", "OK"));
				}
			}
			catch (OperationCanceledException)
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
					Shell.Current.DisplayAlert("Request Timed Out", "The server did not respond in time. Please check your network and ensure the API is running correctly.", "OK"));
			}
			catch (System.Net.Http.HttpRequestException httpEx)
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
					Shell.Current.DisplayAlert("Connection Error", $"Could not connect to the server. Please check the API is running and accessible. Details: {httpEx.Message}", "OK"));
			}
			catch (Exception ex)
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
					Shell.Current.DisplayAlert("An Unexpected Error Occurred", ex.ToString(), "OK"));
			}
		}

		[RelayCommand]
		private async Task CreateAccountAsync()
		{
			await Shell.Current.GoToAsync(nameof(CreateAccountPage));
		}

		[RelayCommand]
		private async Task GoogleLoginAsync()
		{
			try
			{
				var authResult = await WebAuthenticator.Default.AuthenticateAsync(
					new Uri($"{ApiConfig.GetBaseUrl()}/api/externalauth/google-login"),
					new Uri("buildsmart://"));

				if (authResult != null)
				{
					// You can now access the token from authResult.AccessToken
					// Store the token securely
					Application.Current.MainPage = _serviceProvider.GetRequiredService<MainShell>();
				}
			}
			catch (TaskCanceledException)
			{
				// User canceled the authentication
			}
			catch (Exception ex)
			{
				await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
			}
		}

		[RelayCommand]
		private async Task AppleLoginAsync()
		{
			try
			{
				var authResult = await WebAuthenticator.Default.AuthenticateAsync(
					new Uri($"{ApiConfig.GetBaseUrl()}/api/externalauth/apple-login"),
					new Uri("buildsmart://"));

				if (authResult != null)
				{
					// You can now access the token from authResult.AccessToken
					// Store the token securely
					Application.Current.MainPage = _serviceProvider.GetRequiredService<MainShell>();
				}
			}
			catch (TaskCanceledException)
			{
				// User canceled the authentication
			}
			catch (Exception ex)
			{
				await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
			}
		}
	}
}