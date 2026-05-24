using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.MauiMocks;
using BuildSmart.SharedUI.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Threading.Tasks;

namespace BuildSmart.SharedUI.ViewModels
{
    [QueryProperty(nameof(TradesmanId), "TradesmanId")]
    public partial class TradesmanDetailsViewModel : ObservableObject
    {
        private readonly IBuildSmartApiClient _apiClient;

        [ObservableProperty]
        private string _tradesmanId;

        [ObservableProperty]
        private IGetTradesmanDetailsById_TradesmanProfiles? _tradesman;

        [ObservableProperty]
        private bool _isLoading;

        public TradesmanDetailsViewModel(IBuildSmartApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        partial void OnTradesmanIdChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                LoadTradesmanDetailsCommand.Execute(null);
            }
        }

        [RelayCommand]
        public async Task LoadTradesmanDetailsAsync()
        {
            if (IsLoading || string.IsNullOrEmpty(TradesmanId)) return;

            try
            {
                IsLoading = true;
                // Fetching all and filtering client-side as a workaround for schema/filtering issue
                var result = await _apiClient.GetTradesmanDetailsById.ExecuteAsync();

                if (result.Errors.Count == 0 && result.Data?.TradesmanProfiles is not null)
                {
                     // Convert ID string comparison to ignore case and properly match the queried route param
                     Tradesman = result.Data.TradesmanProfiles
                        .FirstOrDefault(t => 
                            string.Equals(t.Id.ToString(), TradesmanId, System.StringComparison.OrdinalIgnoreCase) || 
                            string.Equals(t.User.Id.ToString(), TradesmanId, System.StringComparison.OrdinalIgnoreCase)); 
                }
                else
                {
                    // Handle error
                }
            }
            catch (System.Exception ex)
            {
                 System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task GoBackAsync()
        {
            await AppServiceLocator.Navigation.NavigateToAsync("..");
        }

        [RelayCommand]
        public async Task BookNowAsync()
        {
            if (_tradesman is null) return;

            // Navigate to Booking Page with Tradesman ID and Name
            await AppServiceLocator.Navigation.NavigateToAsync($"{"BookingPage"}?TradesmanId={_tradesman.Id}&TradesmanName={_tradesman.User.FirstName} {_tradesman.User.LastName}");
        }
    }
}






