using BuildSmart.SharedUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using BuildSmart.SharedUI.GraphQL;
using Microsoft.Extensions.Localization;

namespace BuildSmart.SharedUI.ViewModels
{
    public partial class CreateAccountPageViewModel : ObservableObject
    {
        private readonly IBuildSmartApiClient _apiClient;
        private readonly IAuthService _authService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IStringLocalizer<BuildSmart.SharedUI.Resources.AppResources> _localizer;

        [ObservableProperty]
        private string _firstName = string.Empty;

        [ObservableProperty]
        private string _lastName = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _phoneNumber = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _agreeToTerms = false;

        public CreateAccountPageViewModel(
            IBuildSmartApiClient apiClient, 
            IAuthService authService, 
            IServiceProvider serviceProvider,
            IStringLocalizer<BuildSmart.SharedUI.Resources.AppResources> localizer)
        {
            _apiClient = apiClient;
            _authService = authService;
            _serviceProvider = serviceProvider;
            _localizer = localizer;
        }

        [RelayCommand]
        private async Task CreateAccountAsync()
        {
            if (!AgreeToTerms)
            {
                await AppServiceLocator.Alerts.DisplayAlert(
                    _localizer["Validation_Error"] ?? "Validation Error", 
                    _localizer["CreateAccount_AgreeToTermsRequired"] ?? "You must accept the Terms and Conditions and Privacy Policy to register.", 
                    "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) || string.IsNullOrWhiteSpace(Password))
            {
                await AppServiceLocator.Alerts.DisplayAlert("Validation Error", "First name, last name, and password are required.", "OK");
                return;
            }

            if (!Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                await AppServiceLocator.Alerts.DisplayAlert("Validation Error", "Please enter a valid email address.", "OK");
                return;
            }

            string? finalPhone = null;
            if (!string.IsNullOrWhiteSpace(PhoneNumber))
            {
                // Validate Bulgarian phone format (e.g., +359888123456 or 0888123456)
                if (!Regex.IsMatch(PhoneNumber.Trim(), @"^(\+359|0)\d{9}$"))
                {
                    await AppServiceLocator.Alerts.DisplayAlert("Validation Error", "Please enter a valid Bulgarian phone number (e.g., 0888123456 or +359888123456).", "OK");
                    return;
                }
                finalPhone = PhoneNumber.Trim();
            }

            var result = await _apiClient.RegisterUser.ExecuteAsync(FirstName, LastName, Email, Password, finalPhone);

            if (result.Errors.Count == 0)
            {
                // Account created successfully. Automatically log in instead of redirecting to login.
                try
                {
                    await _authService.ClearTokenAsync();
                    
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                    var loginResult = await _apiClient.Login.ExecuteAsync(Email, Password, cts.Token);

                    if (loginResult.Errors.Count == 0 && !string.IsNullOrEmpty(loginResult.Data?.Login))
                    {
                        var token = loginResult.Data.Login;
                        await _authService.SaveTokenAsync(token);

                        // Notify Blazor that the user is now authenticated
                        var authStateProvider = _serviceProvider.GetService(typeof(Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider)) as BuildSmart.SharedUI.Services.MauiAuthenticationStateProvider;
                        authStateProvider?.NotifyAuthenticationStateChanged();

                        await AppServiceLocator.MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await AppServiceLocator.Navigation.NavigateToAsync("//BlazorHostPage");
                        });
                    }
                    else
                    {
                        // Fallback if auto-login fails
                        await AppServiceLocator.Navigation.NavigateToAsync("//LoginPage");
                    }
                }
                catch
                {
                    // Fallback to login page on any exception during auto-login
                    await AppServiceLocator.Navigation.NavigateToAsync("//LoginPage");
                }
            }
            else
            {
                var errorMsg = string.Join("\n", result.Errors.Select(e => e.Message));
                await AppServiceLocator.Alerts.DisplayAlert("Registration Failed", errorMsg, "OK");
            }
        }
    }
}



