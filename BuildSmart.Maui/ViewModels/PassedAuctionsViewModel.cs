using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.Maui.ViewModels;

public partial class PassedAuctionsViewModel : ObservableObject
{
    private readonly IBuildSmartApiClient _apiClient;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<IGetPassedAuctions_PassedAuctions> PassedAuctions { get; } = new();

    public PassedAuctionsViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [RelayCommand]
    public async Task LoadPassedAuctionsAsync()
    {
        try
        {
            IsLoading = true;
            var result = await _apiClient.GetPassedAuctions.ExecuteAsync();
            
            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            PassedAuctions.Clear();
            if (result.Data?.PassedAuctions != null)
            {
                foreach (var auction in result.Data.PassedAuctions)
                {
                    PassedAuctions.Add(auction);
                }
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RestoreAuctionAsync(object parameter)
    {
        if (parameter is not IGetPassedAuctions_PassedAuctions auction)
        {
            return;
        }

        if (auction.Job == null)
        {
            return;
        }

        bool confirm = await Shell.Current.DisplayAlert("Restore Job", 
            $"Do you want to restore '{auction.Job.Title}' to your active feed?", "Yes", "No");
        
        if (!confirm) return;

        try
        {
            IsLoading = true;
            var result = await _apiClient.RestoreAuction.ExecuteAsync(auction.Job.Id);

            if (result.Errors.Count > 0)
            {
                var errorMsg = string.Join(", ", result.Errors.Select(e => e.Message));
                await Shell.Current.DisplayAlert("Error", errorMsg, "OK");
                return;
            }

            if (result.Data?.RestoreAuction == true)
            {
                PassedAuctions.Remove(auction);
                await Shell.Current.DisplayAlert("Success", "Job restored to active feed.", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Failed", "The server could not restore this job.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task NavigateToDetailsAsync(IGetPassedAuctions_PassedAuctions auction)
    {
        if (auction == null) return;
        // Using same route as FeedPage for consistency
        await Shell.Current.GoToAsync($"{nameof(Views.AuctionHubPage)}?jobId={auction.Job.Id}");
    }
}
