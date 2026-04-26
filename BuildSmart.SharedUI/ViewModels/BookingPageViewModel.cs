using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.MauiMocks;
using BuildSmart.SharedUI.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace BuildSmart.SharedUI.ViewModels
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
            await AppServiceLocator.Alerts.DisplayAlert("Deprecated", "Direct bookings are deprecated. Please use the Auction/Escrow workflow.", "OK");
            await AppServiceLocator.Navigation.NavigateToAsync("..");
        }
    }
}





