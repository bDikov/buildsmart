using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.SharedUI.ViewModels;

public partial class ActiveJobsViewModel : ObservableObject
{
    private readonly IBuildSmartApiClient _apiClient;

    public ActiveJobsViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public ObservableCollection<IGetMyActiveBookings_MyActiveBookings> ActiveBookings { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ActiveBookings.Clear();

            var result = await _apiClient.GetMyActiveBookings.ExecuteAsync();

            if (result.Errors.Count > 0)
            {
                await AppServiceLocator.Alerts.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            if (result.Data?.MyActiveBookings != null)
            {
                foreach (var booking in result.Data.MyActiveBookings)
                {
                    ActiveBookings.Add(booking);
                }
            }
        }
        catch (Exception ex)
        {
            await AppServiceLocator.Alerts.DisplayAlert("System Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ViewBookingDetailsAsync(IGetMyActiveBookings_MyActiveBookings booking)
    {
        // We will create the detailed financial dashboard screen next, 
        // for now just a placeholder or we can pass the booking ID to a new page
        await AppServiceLocator.Navigation.NavigateToAsync($"{"TradesmanBookingDashboardPage"}?bookingId={booking.Id}");
    }
}





