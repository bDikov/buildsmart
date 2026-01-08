using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace BuildSmart.Maui.ViewModels
{
    public partial class FeedPageViewModel : ObservableObject
    {
        private readonly IBuildSmartApiClient _apiClient;

        [ObservableProperty]
        private ObservableCollection<IGetTradesmanProfiles_TradesmanProfiles> _tradesmen = new();

        [ObservableProperty]
        private bool _isLoading;

        public FeedPageViewModel(IBuildSmartApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [RelayCommand]
        public async Task NavigateToDetailsAsync(IGetTradesmanProfiles_TradesmanProfiles tradesman)
        {
            if (tradesman is null) return;

            // Pass the ID to the details page
            await Shell.Current.GoToAsync($"{nameof(Views.TradesmanDetailsPage)}?TradesmanId={tradesman.Id}");
        }

        [RelayCommand]
        public async Task NavigateToWizardAsync()
        {
            await Shell.Current.GoToAsync(nameof(Views.JobWizardPage));
        }

        [RelayCommand]
        public async Task LoadTradesmenAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                var result = await _apiClient.GetTradesmanProfiles.ExecuteAsync();
                
                if (result.Errors.Count == 0 && result.Data?.TradesmanProfiles is not null)
                {
                    Tradesmen.Clear();
                    foreach (var tradesman in result.Data.TradesmanProfiles)
                    {
                        Tradesmen.Add(tradesman);
                    }
                }
                else
                {
                   // Handle error (maybe show a toast/alert in a real app)
                   // Console.WriteLine(result.Errors);
                }
            }
            catch (System.Exception ex)
            {
                // Handle exception
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
