using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

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

    public ObservableCollection<QuestionViewModel> Questions { get; } = new();

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
            
            if (result.Errors.Count == 0 && result.Data?.AuctionById != null)
            {
                Auction = result.Data.AuctionById;
                
                Questions.Clear();
                if (Auction.Questions != null)
                {
                    foreach (var q in Auction.Questions)
                    {
                        Questions.Add(new QuestionViewModel(q));
                    }
                }
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
    private async Task ToggleConversationAsync(QuestionViewModel questionVm)
    {
        if (questionVm == null) return;

        if (questionVm.IsExpanded && !questionVm.HasMoreReplies)
        {
            questionVm.IsExpanded = false;
            return;
        }

        if (!questionVm.IsExpanded || questionVm.HasMoreReplies)
        {
            await LoadMoreRepliesAsync(questionVm);
            questionVm.IsExpanded = true;
        }
    }

    [RelayCommand]
    private async Task LoadMoreRepliesAsync(QuestionViewModel questionVm)
    {
        if (questionVm == null || IsBusy) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.GetQuestionReplies.ExecuteAsync(
                questionVm.Question.Id, 
                questionVm.Replies.Count, 
                5);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            if (result.Data?.QuestionReplies?.Replies != null)
            {
                questionVm.AddReplies(result.Data.QuestionReplies.Replies);
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
    private async Task EditQuestionAsync(IQuestionDetails question)
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
    private async Task EditNestedQuestionAsync(IQuestionReplyDetails reply)
    {
        if (reply == null) return;

        string newText = await Shell.Current.DisplayPromptAsync("Edit Reply", "Update your reply:", "Save", "Cancel", initialValue: reply.QuestionText);
        if (string.IsNullOrWhiteSpace(newText) || newText == reply.QuestionText) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.EditJobQuestion.ExecuteAsync(reply.Id, newText);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

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
    private async Task EditAnswerAsync(IQuestionDetails question)
    {
        if (question == null) return;

        string newAnswer = await Shell.Current.DisplayPromptAsync("Edit Answer", "Update your answer:", "Save", "Cancel", initialValue: question.AnswerText);
        if (string.IsNullOrWhiteSpace(newAnswer) || newAnswer == question.AnswerText) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.EditJobAnswer.ExecuteAsync(question.Id, newAnswer);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

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

        string questionText = await Shell.Current.DisplayPromptAsync("Ask Homeowner", "Your question will be public:", "Ask", "Cancel", "Type your question...");
        if (string.IsNullOrWhiteSpace(questionText)) return;

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

            var result = await _apiClient.AskJobQuestion.ExecuteAsync(profileId.Value, Auction.Job.Id, questionText);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

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
    private async Task ReplyToQuestionAsync(IQuestionDetails question)
    {
        if (question == null) return;
        await ExecuteReplyAsync(question.Id);
    }

    [RelayCommand]
    private async Task ReplyToNestedQuestionAsync(IQuestionReplyDetails reply)
    {
        if (reply == null) return;
        await ExecuteReplyAsync(reply.ParentQuestionId ?? reply.Id);
    }

    private async Task ExecuteReplyAsync(Guid parentQuestionId)
    {
        string replyText = await Shell.Current.DisplayPromptAsync("Reply", "Type your reply:", "Send", "Cancel", "...");
        if (string.IsNullOrWhiteSpace(replyText)) return;

        try
        {
            IsBusy = true;

            var result = await _apiClient.ReplyToJobQuestion.ExecuteAsync(parentQuestionId, replyText);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }
            
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
