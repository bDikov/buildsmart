using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace BuildSmart.Maui.ViewModels
{
    [QueryProperty(nameof(TradesmanId), "TradesmanId")]
    [QueryProperty(nameof(TradesmanName), "TradesmanName")]
    public partial class BookingPageViewModel : ObservableObject
    {
        private readonly IBuildSmartApiClient _apiClient;

        [ObservableProperty]
        private string _tradesmanId;

        [ObservableProperty]
        private string _tradesmanName;

        [ObservableProperty]
        private DateTime _requestedDate = DateTime.Now.AddDays(1);

        [ObservableProperty]
        private string _jobDescription = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        public BookingPageViewModel(IBuildSmartApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        [RelayCommand]
        public async Task ConfirmBookingAsync()
        {
            if (string.IsNullOrEmpty(JobDescription))
            {
                await Shell.Current.DisplayAlert("Error", "Please provide a job description.", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                // 1. Get current user ID
                var userResult = await _apiClient.GetCurrentUser.ExecuteAsync();
                if (userResult.Errors.Count != 0 || userResult.Data?.CurrentUser == null)
                {
                    await Shell.Current.DisplayAlert("Error", "Could not identify current user. Please log in again.", "OK");
                    return;
                }

                var homeownerId = userResult.Data.CurrentUser.Id;

                // 2. Create booking
                var result = await _apiClient.CreateBooking.ExecuteAsync(
                    homeownerId,
                    Guid.Parse(TradesmanId),
                    RequestedDate,
                    JobDescription);

                if (result.Errors.Count == 0)
                {
                    await Shell.Current.DisplayAlert("Success", "Booking request sent!", "OK");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", "Failed to create booking.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
