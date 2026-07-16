using BuildSmart.SharedUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using BuildSmart.SharedUI.GraphQL;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

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

        [ObservableProperty]
        private string _firstNameError = string.Empty;

        [ObservableProperty]
        private string _lastNameError = string.Empty;

        [ObservableProperty]
        private string _emailError = string.Empty;

        [ObservableProperty]
        private string _phoneNumberError = string.Empty;

        [ObservableProperty]
        private string _passwordError = string.Empty;

        [ObservableProperty]
        private string _agreeToTermsError = string.Empty;

        [ObservableProperty]
        private bool _isRegistrationSuccess;

        [ObservableProperty]
        private bool _isBusy;

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

        private string GetLocalizedText(string key, string bgDefault, string enDefault)
        {
            var localized = _localizer[key];
            if (localized.ResourceNotFound)
            {
                var isBg = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("bg", System.StringComparison.OrdinalIgnoreCase);
                return isBg ? bgDefault : enDefault;
            }
            return localized.Value;
        }

        public bool Validate()
        {
            FirstNameError = string.Empty;
            LastNameError = string.Empty;
            EmailError = string.Empty;
            PhoneNumberError = string.Empty;
            PasswordError = string.Empty;
            AgreeToTermsError = string.Empty;

            bool isValid = true;

            // Name validation supporting Cyrillic (Bulgarian) and Latin alphabets, spaces, and hyphens
            var nameRegex = @"^[A-Za-zА-Яа-яЁёІіЇїЄєҐґ\s\-'\u0400-\u04FF]+$";

            if (string.IsNullOrWhiteSpace(FirstName))
            {
                FirstNameError = GetLocalizedText("Validation_FirstName_Required", "Името е задължително.", "First name is required.");
                isValid = false;
            }
            else if (FirstName.Trim().Length < 2 || FirstName.Trim().Length > 50)
            {
                FirstNameError = GetLocalizedText("Validation_FirstName_Length", "Името трябва да бъде между 2 и 50 знака.", "First name must be between 2 and 50 characters.");
                isValid = false;
            }
            else if (!Regex.IsMatch(FirstName.Trim(), nameRegex))
            {
                FirstNameError = GetLocalizedText("Validation_FirstName_Invalid", "Името съдържа невалидни символи.", "First name contains invalid characters.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(LastName))
            {
                LastNameError = GetLocalizedText("Validation_LastName_Required", "Фамилията е задължителна.", "Last name is required.");
                isValid = false;
            }
            else if (LastName.Trim().Length < 2 || LastName.Trim().Length > 50)
            {
                LastNameError = GetLocalizedText("Validation_LastName_Length", "Фамилията трябва да бъде между 2 и 50 знака.", "Last name must be between 2 and 50 characters.");
                isValid = false;
            }
            else if (!Regex.IsMatch(LastName.Trim(), nameRegex))
            {
                LastNameError = GetLocalizedText("Validation_LastName_Invalid", "Фамилията съдържа невалидни символи.", "Last name contains invalid characters.");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                EmailError = GetLocalizedText("Validation_Email_Required", "Имейл адресът е задължителен.", "Email address is required.");
                isValid = false;
            }
            else if (!Regex.IsMatch(Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                EmailError = GetLocalizedText("Validation_Email_Invalid", "Моля, въведете валиден имейл адрес.", "Please enter a valid email address.");
                isValid = false;
            }

            if (!string.IsNullOrWhiteSpace(PhoneNumber))
            {
                var normalized = PhoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
                if (!Regex.IsMatch(normalized, @"^(?:\+359|00359|0)([2-9]\d{7,8}|8[7-9]\d{7}|9[8-9]\d{7})$"))
                {
                    PhoneNumberError = GetLocalizedText("Validation_Phone_Invalid", "Моля, въведете валиден български телефонен номер (напр. 0888123456).", "Please enter a valid Bulgarian phone number (e.g. 0888123456).");
                    isValid = false;
                }
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                PasswordError = GetLocalizedText("Validation_Password_Required", "Паролата е задължителна.", "Password is required.");
                isValid = false;
            }
            else if (Password.Length < 6)
            {
                PasswordError = GetLocalizedText("Validation_Password_Length", "Паролата трябва да бъде поне 6 знака.", "Password must be at least 6 characters.");
                isValid = false;
            }

            if (!AgreeToTerms)
            {
                AgreeToTermsError = GetLocalizedText("Validation_AgreeToTerms_Required", "Трябва да се съгласите с условията, за да продължите.", "You must accept the Terms and Conditions to proceed.");
                isValid = false;
            }

            return isValid;
        }

        [RelayCommand]
        private async Task CreateAccountAsync()
        {
            if (IsBusy) return;

            if (!Validate())
            {
                return;
            }

            try
            {
                IsBusy = true;
                string? finalPhone = null;
                if (!string.IsNullOrWhiteSpace(PhoneNumber))
                {
                    finalPhone = PhoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
                }

                var jsRuntime = _serviceProvider.GetService(typeof(Microsoft.JSInterop.IJSRuntime)) as Microsoft.JSInterop.IJSRuntime;
                var utms = new Dictionary<string, string>();
                if (jsRuntime != null)
                {
                    try
                    {
                        utms = await jsRuntime.InvokeAsync<Dictionary<string, string>>("getSavedUtms");
                    }
                    catch { }
                }

                string? utmSource = utms.TryGetValue("utm_source", out var s) ? s : null;
                string? utmMedium = utms.TryGetValue("utm_medium", out var m) ? m : null;
                string? utmCampaign = utms.TryGetValue("utm_campaign", out var c) ? c : null;
                string? utmContent = utms.TryGetValue("utm_content", out var co) ? co : null;
                string? utmTerm = utms.TryGetValue("utm_term", out var t) ? t : null;

                var result = await _apiClient.RegisterUser.ExecuteAsync(
                    FirstName.Trim(),
                    LastName.Trim(),
                    Email.Trim(),
                    Password,
                    finalPhone,
                    utmSource,
                    utmMedium,
                    utmCampaign,
                    utmContent,
                    utmTerm);

                if (result.Errors.Count == 0)
                {
                    IsRegistrationSuccess = true;
                }
                else
                {
                    var errorMsg = string.Join("\n", result.Errors.Select(e => e.Message));
                    await AppServiceLocator.Alerts.DisplayAlert("Registration Failed", errorMsg, "OK");
                }
            }
            catch (System.Exception ex)
            {
                await AppServiceLocator.Alerts.DisplayAlert("Error", $"An unexpected error occurred: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}



