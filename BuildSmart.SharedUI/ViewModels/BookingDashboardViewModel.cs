using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.MauiMocks;
using BuildSmart.SharedUI.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.SharedUI.ViewModels;

[QueryProperty(nameof(BookingId), "BookingId")]
public partial class BookingDashboardViewModel : ObservableObject
{
    private readonly IBuildSmartApiClient _apiClient;

    public BookingDashboardViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [ObservableProperty]
    private string? _bookingId;

    [ObservableProperty]
    private IGetProjectById_ProjectById_JobPosts_Bids? _booking; // We will use a mock type or fetch the actual booking if we wrote the query

    [ObservableProperty]
    private bool _isBusy;

    partial void OnBookingIdChanged(string? value)
    {
        // Load booking details (skipping query implementation for now)
    }

    [RelayCommand]
    private async Task ApproveMilestoneAsync(string milestoneId)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            
            var result = await _apiClient.ApproveMilestone.ExecuteAsync(Guid.Parse(milestoneId));
            
            if (result.Errors.Count > 0)
            {
                await AppServiceLocator.Alerts.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            await AppServiceLocator.Alerts.DisplayAlert("Success", "Milestone approved! Funds will be released.", "OK");
            // Refresh logic here
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
}



