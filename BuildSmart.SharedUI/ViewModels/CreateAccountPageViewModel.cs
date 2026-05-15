using BuildSmart.SharedUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using BuildSmart.SharedUI.GraphQL;

namespace BuildSmart.SharedUI.ViewModels
{
    public partial class CreateAccountPageViewModel : ObservableObject
    {
        private readonly IBuildSmartApiClient _apiClient;

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

        public CreateAccountPageViewModel(IBuildSmartApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [RelayCommand]
        private async Task CreateAccountAsync()
        {
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
                await AppServiceLocator.Navigation.NavigateToAsync("//LoginPage");
            }
            else
            {
                var errorMsg = string.Join("\n", result.Errors.Select(e => e.Message));
                await AppServiceLocator.Alerts.DisplayAlert("Registration Failed", errorMsg, "OK");
            }
        }
    }
}



