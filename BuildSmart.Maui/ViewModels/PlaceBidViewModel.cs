using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuildSmart.Maui.ViewModels;

[QueryProperty(nameof(JobId), "jobId")]
public partial class PlaceBidViewModel : ObservableObject
{
    private readonly IBuildSmartApiClient _apiClient;

    public PlaceBidViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
        EarliestStartDate = DateTime.Today.AddDays(1); // Default to tomorrow
    }

    [ObservableProperty]
    private string? _jobId;

    [ObservableProperty]
    private decimal _amount;

    [ObservableProperty]
    private string _comment = string.Empty;

    [ObservableProperty]
    private DateTime _earliestStartDate;

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    private async Task SubmitBidAsync()
    {
        if (string.IsNullOrEmpty(JobId) || !Guid.TryParse(JobId, out var parsedJobId))
        {
            await Shell.Current.DisplayAlert("Error", "Invalid Job ID.", "OK");
            return;
        }

        if (Amount <= 0)
        {
            await Shell.Current.DisplayAlert("Validation", "Please enter a valid bid amount.", "OK");
            return;
        }

        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // Fetch current tradesman profile ID
            var userResult = await _apiClient.GetCurrentUser.ExecuteAsync();
            var tradesmanIdStr = userResult.Data?.CurrentUser?.TradesmanProfile?.Id;
            
            if (string.IsNullOrEmpty(tradesmanIdStr) || !Guid.TryParse(tradesmanIdStr, out var tradesmanId))
            {
                await Shell.Current.DisplayAlert("Error", "Could not verify your Tradesman Profile.", "OK");
                return;
            }

            var input = new SubmitBidInput
            {
                JobPostId = parsedJobId,
                TradesmanProfileId = tradesmanId,
                Currency = "USD",
                Comment = Comment,
                EarliestStartDate = EarliestStartDate
            };

            var result = await _apiClient.SubmitBid.ExecuteAsync(input);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            await Shell.Current.DisplayAlert("Success", "Your bid has been placed successfully!", "OK");
            
            // Navigate back
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

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
