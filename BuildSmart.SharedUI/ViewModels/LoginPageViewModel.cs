using BuildSmart.SharedUI.GraphQL;
using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.MauiMocks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuildSmart.SharedUI.ViewModels
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
					await AppServiceLocator.MainThread.InvokeOnMainThreadAsync(() =>
						AppServiceLocator.Alerts.DisplayAlert("Login Failed", errorMessage, "OK"));
					return;
				}

				if (!string.IsNullOrEmpty(result.Data?.Login))
				{
					var token = result.Data.Login;
					await _authService.SaveTokenAsync(token);

					var userRole = _authService.GetUserRoleFromToken(token);

                    // Notify Blazor that the user is now authenticated
                    var authStateProvider = _serviceProvider.GetService(typeof(Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider)) as BuildSmart.SharedUI.Services.MauiAuthenticationStateProvider;
                    authStateProvider?.NotifyAuthenticationStateChanged();

					await AppServiceLocator.MainThread.InvokeOnMainThreadAsync(async () =>
					{
                        // Application navigation removed for shared UI
                        await AppServiceLocator.Navigation.NavigateToAsync("//BlazorHostPage");
					});
				}
				else
				{
					await AppServiceLocator.MainThread.InvokeOnMainThreadAsync(() =>
						AppServiceLocator.Alerts.DisplayAlert("Login Failed", "Received an empty or invalid response from the server.", "OK"));
				}
			}
			catch (OperationCanceledException)
			{
				await AppServiceLocator.MainThread.InvokeOnMainThreadAsync(() =>
					AppServiceLocator.Alerts.DisplayAlert("Request Timed Out", "The server did not respond in time. Please check your network and ensure the API is running correctly.", "OK"));
			}
			catch (System.Net.Http.HttpRequestException httpEx)
			{
				await AppServiceLocator.MainThread.InvokeOnMainThreadAsync(() =>
					AppServiceLocator.Alerts.DisplayAlert("Connection Error", $"Could not connect to the server. Please check the API is running and accessible. Details: {httpEx.Message}", "OK"));
			}
			catch (Exception ex)
			{
				await AppServiceLocator.MainThread.InvokeOnMainThreadAsync(() =>
					AppServiceLocator.Alerts.DisplayAlert("An Unexpected Error Occurred", ex.ToString(), "OK"));
			}
		}

		[RelayCommand]
		private async Task CreateAccountAsync()
		{
			await AppServiceLocator.Navigation.NavigateToAsync("CreateAccountPage");
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
					// Application navigation removed for shared UI
                    await AppServiceLocator.Navigation.NavigateToAsync("//BlazorHostPage");
				}
			}
			catch (TaskCanceledException)
			{
				// User canceled the authentication
			}
			catch (Exception ex)
			{
				await AppServiceLocator.Alerts.DisplayAlert("Error", ex.Message, "OK");
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
					// Application navigation removed for shared UI
                    await AppServiceLocator.Navigation.NavigateToAsync("//BlazorHostPage");
				}
			}
			catch (TaskCanceledException)
			{
				// User canceled the authentication
			}
			catch (Exception ex)
			{
				await AppServiceLocator.Alerts.DisplayAlert("Error", ex.Message, "OK");
			}
		}
	}
}




