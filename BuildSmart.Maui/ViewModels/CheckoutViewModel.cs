using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuildSmart.Maui.ViewModels;

[QueryProperty(nameof(Bid), "Bid")]
public partial class CheckoutViewModel : ObservableObject
{
    private readonly IBuildSmartApiClient _apiClient;

    public CheckoutViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [ObservableProperty]
    private object? _bid;

    public Guid BidId { get; private set; }
    public decimal BidAmount { get; private set; }
    public string TradesmanName { get; private set; } = string.Empty;
    public string TradesmanPhoto { get; private set; } = string.Empty;
    public string BidComment { get; private set; } = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public decimal PlatformFee => BidAmount * 0.03m;
    public decimal TotalCharge => BidAmount + PlatformFee;

    partial void OnBidChanged(object? value)
    {
        if (value is IGetProjectsForReview_ProjectsForReview_JobPosts_Bids prBid)
        {
            BidId = prBid.Id;
            BidAmount = prBid.Amount.Total;
            TradesmanName = prBid.TradesmanProfile.User.FirstName;
            TradesmanPhoto = prBid.TradesmanProfile.User.ProfilePictureUrl ?? "";
            BidComment = prBid.Comment ?? "";
        }
        else if (value is IGetBidDetails_BidDetailsById bdBid)
        {
            BidId = bdBid.Id;
            BidAmount = bdBid.Amount.Total;
            TradesmanName = bdBid.TradesmanProfile.User.FirstName;
            TradesmanPhoto = bdBid.TradesmanProfile.User.ProfilePictureUrl ?? "";
            BidComment = bdBid.Comment ?? "";
        }

        OnPropertyChanged(nameof(BidId));
        OnPropertyChanged(nameof(BidAmount));
        OnPropertyChanged(nameof(TradesmanName));
        OnPropertyChanged(nameof(TradesmanPhoto));
        OnPropertyChanged(nameof(BidComment));
        OnPropertyChanged(nameof(PlatformFee));
        OnPropertyChanged(nameof(TotalCharge));
    }

    [RelayCommand]
    private async Task AcceptBidAsync()
    {
        if (Bid == null || BidId == Guid.Empty) return;

        bool confirm = await Shell.Current.DisplayAlert("Confirm Payment", 
            $"Are you sure you want to deposit {TotalCharge:C2} into escrow and hire this tradesman?", 
            "Yes, Deposit Funds", "Cancel");

        if (!confirm) return;

        if (IsBusy) return;

        try
        {
            IsBusy = true;
            
            var result = await _apiClient.AcceptBid.ExecuteAsync(BidId);
            
            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            await Shell.Current.DisplayAlert("Success", "Funds have been securely deposited into escrow! The tradesman has been hired.", "OK");
            
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
