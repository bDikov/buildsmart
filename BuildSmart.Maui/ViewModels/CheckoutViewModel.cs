using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuildSmart.Maui.ViewModels;

public partial class CheckoutViewModel : ObservableObject, IQueryAttributable
{
    private readonly IBuildSmartApiClient _apiClient;

    public CheckoutViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [ObservableProperty]
    private IGetProjectsForReview_ProjectsForReview_JobPosts_Bids? _bid;

    [ObservableProperty]
    private bool _isBusy;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Bid", out var bidObj) && bidObj is IGetProjectsForReview_ProjectsForReview_JobPosts_Bids bid)
        {
            Bid = bid;
        }
    }

    public decimal PlatformFee => Bid != null ? Bid.Amount.Total * 0.03m : 0;
    public decimal TotalCharge => Bid != null ? Bid.Amount.Total + PlatformFee : 0;

    partial void OnBidChanged(IGetProjectsForReview_ProjectsForReview_JobPosts_Bids? value)
    {
        OnPropertyChanged(nameof(PlatformFee));
        OnPropertyChanged(nameof(TotalCharge));
    }

    [RelayCommand]
    private async Task AcceptBidAsync()
    {
        if (Bid == null) return;

        bool confirm = await Shell.Current.DisplayAlert("Confirm Payment", 
            $"Are you sure you want to deposit {TotalCharge:C2} into escrow and hire this tradesman?", 
            "Yes, Deposit Funds", "Cancel");

        if (!confirm) return;

        if (IsBusy) return;

        try
        {
            IsBusy = true;
            
            // Assume we call stripe here or mock it, then call our backend to Accept
            var result = await _apiClient.AcceptBid.ExecuteAsync(Bid.Id);
            
            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            await Shell.Current.DisplayAlert("Success", "Funds have been securely deposited into escrow! The tradesman has been hired.", "OK");
            
            // Go back to the project page
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("System Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
