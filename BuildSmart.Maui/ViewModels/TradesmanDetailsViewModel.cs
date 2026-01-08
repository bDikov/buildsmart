using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using System.Threading.Tasks;

namespace BuildSmart.Maui.ViewModels
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
                     Tradesman = result.Data.TradesmanProfiles
                        .FirstOrDefault(t => t.Id.ToString() == TradesmanId || t.User.Id.ToString() == TradesmanId); 
                        // Checking both ID and UserID just in case, though ID should match TradesmanProfileId
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
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        public async Task BookNowAsync()
        {
            if (_tradesman is null) return;

            // Navigate to Booking Page with Tradesman ID and Name
            await Shell.Current.GoToAsync($"{nameof(Views.BookingPage)}?TradesmanId={_tradesman.Id}&TradesmanName={_tradesman.User.FirstName} {_tradesman.User.LastName}");
        }
    }
}
