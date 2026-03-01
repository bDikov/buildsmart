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
    private Guid? _currentTradesmanProfileId;

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

            // Fetch current user's profile ID to control Edit button visibility
            var userResult = await _apiClient.GetCurrentUser.ExecuteAsync();
            CurrentTradesmanProfileId = userResult.Data?.CurrentUser?.TradesmanProfile?.Id;

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

    [RelayCommand]
    private async Task EditQuestionAsync(IGetAuctionById_AuctionById_Questions question)
    {
        if (question == null) return;

        string newText = await Shell.Current.DisplayPromptAsync("Edit Question", "Update your public question:", "Save", "Cancel", initialValue: question.QuestionText);
        if (string.IsNullOrWhiteSpace(newText) || newText == question.QuestionText) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.EditJobQuestion.ExecuteAsync(question.Id, newText);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            await Shell.Current.DisplayAlert("Success", "Question updated.", "OK");
            
            if (Guid.TryParse(JobId, out var id))
            {
                await LoadAuctionAsync(id);
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
    private async Task AskQuestionAsync()
    {
        if (Auction == null) return;

        string question = await Shell.Current.DisplayPromptAsync("Ask Homeowner", "Your question will be public:", "Ask", "Cancel", "Type your question...");
        if (string.IsNullOrWhiteSpace(question)) return;

        try
        {
            IsBusy = true;
            
            // Get Current User Profile Id
            var userResult = await _apiClient.GetCurrentUser.ExecuteAsync();
            var profileId = userResult.Data?.CurrentUser?.TradesmanProfile?.Id;

            if (profileId == null)
            {
                await Shell.Current.DisplayAlert("Error", "Tradesman profile not found.", "OK");
                return;
            }

            var result = await _apiClient.AskJobQuestion.ExecuteAsync(profileId.Value, Auction.Job.Id, question);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            await Shell.Current.DisplayAlert("Success", "Question posted and is now public.", "OK");
            
            if (Guid.TryParse(JobId, out var id))
            {
                await LoadAuctionAsync(id);
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
