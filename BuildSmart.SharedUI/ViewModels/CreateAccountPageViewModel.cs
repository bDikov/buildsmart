using BuildSmart.SharedUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
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
        private string _password = string.Empty;

        public CreateAccountPageViewModel(IBuildSmartApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [RelayCommand]
        private async Task CreateAccountAsync()
        {
            // Input validation can be added here

            var result = await _apiClient.RegisterUser.ExecuteAsync(FirstName, LastName, Email, Password);

            if (result.Errors.Count == 0)
            {
                // Navigate to the login page or detailed view page
                await AppServiceLocator.Navigation.NavigateToAsync("//LoginPage");
            }
            else
            {
                // Handle errors (e.g., display an alert)
            }
        }
    }
}



