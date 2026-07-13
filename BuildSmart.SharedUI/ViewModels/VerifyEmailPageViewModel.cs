using BuildSmart.SharedUI.GraphQL;
using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.MauiMocks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BuildSmart.SharedUI.ViewModels
{
    [QueryProperty(nameof(Email), "email")]
    public partial class VerifyEmailPageViewModel : ObservableObject
    {
        private readonly IBuildSmartApiClient _apiClient;
        private readonly Microsoft.JSInterop.IJSRuntime _jsRuntime;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _code = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        public VerifyEmailPageViewModel(IBuildSmartApiClient apiClient, Microsoft.JSInterop.IJSRuntime jsRuntime)
        {
            _apiClient = apiClient;
            _jsRuntime = jsRuntime;
        }

        [RelayCommand]
        private async Task VerifyEmailAsync()
        {
            if (IsBusy) return;

            if (string.IsNullOrWhiteSpace(Email))
            {
                await AppServiceLocator.Alerts.DisplayAlert("Validation Error", "Email is required.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(Code) || Code.Trim().Length != 6)
            {
                await AppServiceLocator.Alerts.DisplayAlert("Validation Error", "Please enter the 6-digit verification code.", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                var result = await _apiClient.VerifyEmail.ExecuteAsync(Email.Trim(), Code.Trim());

                if (result.Errors.Count > 0)
                {
                    var errorMsg = result.Errors[0].Message;
                    await AppServiceLocator.Alerts.DisplayAlert("Verification Failed", errorMsg, "OK");
                    return;
                }

                if (result.Data?.VerifyEmail == true)
                {
                    try
                    {
                        await _jsRuntime.InvokeVoidAsync("pushToDataLayer", "registration_success", new { email = Email.Trim() });
                        await _jsRuntime.InvokeVoidAsync("posthog.capture", "registration_success", new { email = Email.Trim() });
                    }
                    catch { }

                    await AppServiceLocator.Alerts.DisplayAlert("Success", "Your email has been verified successfully. You can now log in.", "OK");
                    await AppServiceLocator.Navigation.NavigateToAsync("//LoginPage");
                }
                else
                {
                    await AppServiceLocator.Alerts.DisplayAlert("Verification Failed", "Invalid or expired verification code.", "OK");
                }
            }
            catch (System.Exception ex)
            {
                await AppServiceLocator.Alerts.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ResendCodeAsync()
        {
            if (IsBusy) return;

            if (string.IsNullOrWhiteSpace(Email))
            {
                await AppServiceLocator.Alerts.DisplayAlert("Validation Error", "Email is required to resend the code.", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                var result = await _apiClient.ResendVerificationCode.ExecuteAsync(Email.Trim());

                if (result.Errors.Count > 0)
                {
                    var errorMsg = result.Errors[0].Message;
                    await AppServiceLocator.Alerts.DisplayAlert("Failed to Send Code", errorMsg, "OK");
                    return;
                }

                if (result.Data?.ResendVerificationCode == true)
                {
                    await AppServiceLocator.Alerts.DisplayAlert("Success", "A new verification code has been sent to your email.", "OK");
                }
                else
                {
                    await AppServiceLocator.Alerts.DisplayAlert("Failed to Send Code", "Could not send verification code at this time.", "OK");
                }
            }
            catch (System.Exception ex)
            {
                await AppServiceLocator.Alerts.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
