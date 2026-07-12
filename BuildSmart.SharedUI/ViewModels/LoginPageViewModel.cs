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
					var error = result.Errors.FirstOrDefault();
					var errorMessage = error?.Message ?? "Unknown GraphQL error.";
					var errorCode = error?.Code;

					await AppServiceLocator.MainThread.InvokeOnMainThreadAsync(async () =>
					{
						if (errorCode == "AUTH_EMAIL_NOT_VERIFIED" || errorMessage.Contains("verify", StringComparison.OrdinalIgnoreCase))
						{
							bool verifyNow = await AppServiceLocator.Alerts.DisplayAlert(
								"Email Not Verified",
								"Your email is not verified. Would you like to verify it now?",
								"Yes", "No");
							if (verifyNow)
							{
								await AppServiceLocator.Navigation.NavigateToAsync($"VerifyEmailPage?email={Email}");
							}
						}
						else
						{
							await AppServiceLocator.Alerts.DisplayAlert("Login Failed", errorMessage, "OK");
						}
					});
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
						string destination = "/";
						var navManager = _serviceProvider.GetService(typeof(Microsoft.AspNetCore.Components.NavigationManager)) as Microsoft.AspNetCore.Components.NavigationManager;
						if (navManager != null)
						{
							try
							{
								var uri = navManager.ToAbsoluteUri(navManager.Uri);
								var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
								var returnUrlKey = System.Linq.Enumerable.FirstOrDefault(query.Keys, k => k.Equals("ReturnUrl", StringComparison.OrdinalIgnoreCase));
								if (returnUrlKey != null)
								{
									destination = query[returnUrlKey].FirstOrDefault() ?? "/";
								}
							}
							catch (InvalidOperationException)
							{
								// NavigationManager is not initialized yet because login is triggered from a native MAUI page
								destination = "/";
							}
						}

						// Prevent open redirect vulnerabilities
						if (!destination.StartsWith("/") || destination.StartsWith("//"))
						{
							destination = "/";
						}

						if (navManager != null)
						{
							navManager.NavigateTo(destination, forceLoad: true);
						}
						else
						{
							if (destination == "/")
							{
								destination = "//BlazorHostPage";
							}
							await AppServiceLocator.Navigation.NavigateToAsync(destination);
						}
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
				var token = await _authService.AuthenticateWithGoogleAsync();

				if (!string.IsNullOrEmpty(token))
				{
					await _authService.SaveTokenAsync(token);

                    var authStateProvider = _serviceProvider.GetService(typeof(Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider)) as BuildSmart.SharedUI.Services.MauiAuthenticationStateProvider;
                    authStateProvider?.NotifyAuthenticationStateChanged();

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
				var token = await _authService.AuthenticateWithAppleAsync();

				if (!string.IsNullOrEmpty(token))
				{
					await _authService.SaveTokenAsync(token);

                    var authStateProvider = _serviceProvider.GetService(typeof(Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider)) as BuildSmart.SharedUI.Services.MauiAuthenticationStateProvider;
                    authStateProvider?.NotifyAuthenticationStateChanged();

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




