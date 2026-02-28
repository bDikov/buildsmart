using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuildSmart.Maui.ViewModels;

[QueryProperty(nameof(JobId), "jobId")]
public partial class AuctionHubViewModel : ObservableObject
{
    private readonly IBuildSmartApiClient _apiClient;

    public AuctionHubViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [ObservableProperty]
    private string? _jobId;

    [ObservableProperty]
    private IGetAuctionById_AuctionById? _auction;

    [ObservableProperty]
    private bool _isBusy;

    partial void OnJobIdChanged(string? value)
    {
        if (Guid.TryParse(value, out var id))
        {
            LoadAuctionAsync(id);
        }
    }

    private async Task LoadAuctionAsync(Guid jobId)
    {
        try
        {
            IsBusy = true;
            var result = await _apiClient.GetAuctionById.ExecuteAsync(jobId);
            
            if (result.Errors.Count == 0)
            {
                Auction = result.Data?.AuctionById;
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

    [RelayCommand]
    private async Task BackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
