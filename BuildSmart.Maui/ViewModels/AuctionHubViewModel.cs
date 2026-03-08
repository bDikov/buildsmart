using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.Maui.ViewModels;

[QueryProperty(nameof(JobId), "jobId")]
public partial class AuctionHubViewModel : ObservableObject
{
    private readonly IBuildSmartApiClient _apiClient;
    private readonly Services.SignalRService _signalRService;

    public AuctionHubViewModel(IBuildSmartApiClient apiClient, Services.SignalRService signalRService)
    {
        _apiClient = apiClient;
        _signalRService = signalRService;
    }

    public async Task InitializeAsync()
    {
        _signalRService.QuestionUpdated += OnQuestionUpdated;
        _signalRService.NewReplyReceived += OnNewReplyReceived;
        
        if (!string.IsNullOrEmpty(JobId))
        {
            await _signalRService.ConnectAsync();
            await _signalRService.JoinAuctionGroupAsync(JobId);
        }
    }

    public async Task CleanupAsync()
    {
        _signalRService.QuestionUpdated -= OnQuestionUpdated;
        _signalRService.NewReplyReceived -= OnNewReplyReceived;
        
        if (!string.IsNullOrEmpty(JobId))
        {
            await _signalRService.LeaveAuctionGroupAsync(JobId);
        }
    }

    private void OnQuestionUpdated(System.Text.Json.JsonElement payload)
    {
        if (payload.TryGetProperty("id", out var idProp) && Guid.TryParse(idProp.GetString(), out var id))
        {
            var vm = Questions.FirstOrDefault(q => q.Question?.Id == id);
            if (vm != null)
            {
                vm.UpdateQuestion(new QuestionUpdateWrapper(vm.Question!, payload));
            }
        }
    }

    private void OnNewReplyReceived(System.Text.Json.JsonElement payload)
    {
        var parentProp = payload.EnumerateObject().FirstOrDefault(p => p.Name.Equals("parentQuestionId", StringComparison.OrdinalIgnoreCase)).Value;
        if (parentProp.ValueKind != System.Text.Json.JsonValueKind.Undefined && parentProp.ValueKind != System.Text.Json.JsonValueKind.Null && Guid.TryParse(parentProp.GetString(), out var parentId))
        {
            var vm = Questions.FirstOrDefault(q => q.Question?.Id == parentId);
            if (vm != null)
            {
                vm.AddReply(new ReplyWrapper(payload));
            }
        }
    }

    private class QuestionUpdateWrapper : IQuestionDetails
    {
        private readonly IQuestionDetails _original;
        private readonly System.Text.Json.JsonElement _payload;

        public QuestionUpdateWrapper(IQuestionDetails original, System.Text.Json.JsonElement payload)
        {
            _original = original;
            _payload = payload;
        }

        public Guid Id => _original.Id;
        public Guid JobPostId => _original.JobPostId;
        public string QuestionText => _payload.TryGetProperty("questionText", out var p) ? p.GetString() ?? _original.QuestionText : _original.QuestionText;
        public string? AnswerText => _payload.TryGetProperty("answerText", out var p) ? p.GetString() : _original.AnswerText;
        public DateTimeOffset? AnsweredAt => _original.AnsweredAt;
        public bool IsAnswered => _original.IsAnswered;
        public bool IsEdited => _payload.TryGetProperty("isEdited", out var p) ? p.GetBoolean() : _original.IsEdited;
        public bool IsAnswerEdited => _payload.TryGetProperty("isAnswerEdited", out var p) ? p.GetBoolean() : _original.IsAnswerEdited;
        public bool IsEditable => _original.IsEditable;
        public bool IsAnswerEditable => _original.IsAnswerEditable;
        public Guid? AuthorId => _original.AuthorId;
        public Guid? TradesmanProfileId => _original.TradesmanProfileId;
        public DateTimeOffset CreatedAt => _original.CreatedAt;
        public DateTimeOffset UpdatedAt => _payload.TryGetProperty("updatedAt", out var p) && p.TryGetDateTimeOffset(out var dt) ? dt : _original.UpdatedAt;
        public int ReplyCount => _original.ReplyCount;
        public IGetProjectsForReview_ProjectsForReview_JobPosts_Questions_TradesmanProfile? TradesmanProfile => (IGetProjectsForReview_ProjectsForReview_JobPosts_Questions_TradesmanProfile?)_original.TradesmanProfile;
        public IGetProjectsForReview_ProjectsForReview_JobPosts_Questions_Author? Author => (IGetProjectsForReview_ProjectsForReview_JobPosts_Questions_Author?)_original.Author;
    }

    private class ReplyWrapper : IQuestionReplyDetails
    {
        private readonly System.Text.Json.JsonElement _payload;

        public ReplyWrapper(System.Text.Json.JsonElement payload)
        {
            _payload = payload;
        }

        private System.Text.Json.JsonElement? GetProp(string name)
        {
            var prop = _payload.EnumerateObject().FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return prop.Value.ValueKind != System.Text.Json.JsonValueKind.Undefined ? prop.Value : null;
        }

        public Guid Id => GetProp("id") is var p && p != null ? Guid.Parse(p.Value.GetString()!) : Guid.Empty;
        public Guid? ParentQuestionId => GetProp("parentQuestionId") is var p && p != null && p.Value.ValueKind != System.Text.Json.JsonValueKind.Null ? Guid.Parse(p.Value.GetString()!) : null;
        public Guid? TradesmanProfileId => GetProp("tradesmanProfileId") is var p && p != null && p.Value.ValueKind != System.Text.Json.JsonValueKind.Null ? Guid.Parse(p.Value.GetString()!) : null;
        public Guid? AuthorId => GetProp("authorId") is var p && p != null && p.Value.ValueKind != System.Text.Json.JsonValueKind.Null ? Guid.Parse(p.Value.GetString()!) : null;
        public string QuestionText => GetProp("questionText") is var p && p != null ? p.Value.GetString() ?? "" : "";
        public DateTimeOffset CreatedAt => GetProp("createdAt") is var p && p != null && p.Value.TryGetDateTimeOffset(out var dt) ? dt : DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt => GetProp("updatedAt") is var p && p != null && p.Value.TryGetDateTimeOffset(out var dt) ? dt : DateTimeOffset.UtcNow;
        public bool IsEdited => false;
        public bool IsEditable => false;
        public IGetQuestionReplies_QuestionReplies_Replies_TradesmanProfile? TradesmanProfile => null;
        public IGetQuestionReplies_QuestionReplies_Replies_Author? Author => null;
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
                        Questions.Add(new QuestionViewModel(q, LoadMoreRepliesAsync));
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
    private async Task ToggleConversationAsync(QuestionViewModel questionVm)
    {
        if (questionVm == null) return;
        await questionVm.ToggleConversationCommand.ExecuteAsync(null);
    }

            [RelayCommand]
            private async Task BackAsync()    {
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

            var updatedQuestion = result.Data?.EditJobQuestion;
            if (updatedQuestion != null)
            {
                var vm = Questions.FirstOrDefault(q => q.Question?.Id == question.Id);
                if (vm != null)
                {
                    vm.UpdateQuestion(updatedQuestion);
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

            var updatedQuestion = result.Data?.EditJobAnswer;
            if (updatedQuestion != null)
            {
                var vm = Questions.FirstOrDefault(q => q.Question?.Id == question.Id);
                if (vm != null)
                {
                    vm.UpdateAnswer(updatedQuestion);
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
            
            var newReply = result.Data?.ReplyToJobQuestion;
            if (newReply != null)
            {
                var vm = Questions.FirstOrDefault(q => q.Question?.Id == parentQuestionId);
                if (vm != null)
                {
                    vm.AddReply(newReply);
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
}
