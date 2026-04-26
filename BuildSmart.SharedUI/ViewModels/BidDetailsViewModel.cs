using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.MauiMocks;
using BuildSmart.SharedUI.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.SharedUI.ViewModels;

[QueryProperty(nameof(JobPostId), "jobPostId")]
[QueryProperty(nameof(BidId), "bidId")]
public partial class BidDetailsViewModel : ObservableObject
{
    private readonly IBuildSmartApiClient _apiClient;

    public BidDetailsViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [ObservableProperty]
    private string? _jobPostId;

    [ObservableProperty]
    private string? _bidId;

    [ObservableProperty]
    private IGetBidDetails_BidDetailsById? _bid;

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<IGetBidDetails_BidDetailsById_BidItems> BidItems { get; } = new();

    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(JobPostId) || string.IsNullOrEmpty(BidId))
            return;

        if (IsBusy) return;

        try
        {
            IsBusy = true;
            BidItems.Clear();

            var result = await _apiClient.GetBidDetails.ExecuteAsync(Guid.Parse(BidId));

            if (result.Errors.Count == 0 && result.Data?.BidDetailsById != null)
            {
                var bid = result.Data.BidDetailsById;
                Bid = bid;
                foreach (var item in bid.BidItems)
                {
                    BidItems.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            await AppServiceLocator.Alerts.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AcceptBidAsync()
    {
        if (Bid == null) return;

        try
        {
            await AppServiceLocator.Navigation.NavigateToAsync("CheckoutPage", new Dictionary<string, object>
            {
                { "Bid", Bid }
            });
        }
        catch (Exception ex)
        {
            await AppServiceLocator.Alerts.DisplayAlert("Navigation Error", ex.Message, "OK");
        }
    }
}






