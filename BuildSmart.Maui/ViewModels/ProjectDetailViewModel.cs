using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BuildSmart.Maui.Views;
using BuildSmart.Maui.Services;
using System.Security.Claims;
using System.Collections.ObjectModel;

namespace BuildSmart.Maui.ViewModels;

public partial class ProjectDetailViewModel : ObservableObject, IQueryAttributable
{
	private readonly IBuildSmartApiClient _apiClient;
	private readonly SignalRService _signalRService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private bool _hasLoaded;

    [ObservableProperty]
    private bool _isLoading;

    private readonly SemaphoreSlim _reloadSemaphore = new(1, 1);
    private DateTime _lastReloadTime = DateTime.MinValue;

    public ObservableCollection<JobPostViewModel> JobPosts { get; } = new();

	public ProjectDetailViewModel(IBuildSmartApiClient apiClient, SignalRService signalRService, IAuthService authService)
	{
		_apiClient = apiClient;
		_signalRService = signalRService;
        _authService = authService;

		_signalRService.NotificationReceived += OnNotificationReceived;
        _ = DetectRoleAsync();
	}

    private async Task DetectRoleAsync()
    {
        try
        {
            var token = await _authService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                var userId = _authService.GetUserIdFromToken(token);
                if (userId != null)
                {
                    CurrentUserId = userId;
                }

                var role = _authService.GetUserRoleFromToken(token);
                IsHomeowner = string.Equals(role, "HOMEOWNER", StringComparison.OrdinalIgnoreCase) || 
                              string.Equals(role, "Homeowner", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch { /* Silently fail, default to false */ }
    }

	private void OnNotificationReceived(string title, string message, object? data)
	{
		if (Project != null)
		{
			MainThread.BeginInvokeOnMainThread(async () => await ReloadProjectDebouncedAsync());
		}
	}

    private async Task ReloadProjectDebouncedAsync()
    {
        if ((DateTime.UtcNow - _lastReloadTime).TotalSeconds < 2) return;
        await ReloadProjectAsync();
    }

	private async Task ReloadProjectAsync()
	{
		if (Project == null || IsLoading) return;

		try
		{
            await _reloadSemaphore.WaitAsync();
            IsLoading = true;
            _lastReloadTime = DateTime.UtcNow;

			var result = await _apiClient.GetProjectById.ExecuteAsync(Project.Id);
			if (result.Data?.ProjectById != null)
			{
				Project = result.Data.ProjectById;
                SyncJobPosts();
                HasLoaded = true;
			}
		}
		catch { /* Silently fail reload */ }
        finally
        {
            IsLoading = false;
            _reloadSemaphore.Release();
        }
	}

	[ObservableProperty]
	private IProjectDetails? _project;

	[ObservableProperty]
	private bool _isBusy;

    [ObservableProperty]
    private bool _isHomeowner;

    [ObservableProperty]
    private Guid? _currentUserId;

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("Project", out var projectObj))
		{
            if (projectObj is IProjectDetails project)
            {
                // CRITICAL: Clear current state first to prevent layout collisions
                HasLoaded = false;
                Project = null; 

                // Use a background task to allow navigation to complete smoothly
                Task.Run(async () => {
                    await Task.Delay(300); // Give the UI thread time to breathe
                    
                    MainThread.BeginInvokeOnMainThread(() => {
                        Project = project;
                        SyncJobPosts();
                        HasLoaded = true;
                    });
                });
            }
		}
	}

    private void SyncJobPosts()
    {
        JobPosts.Clear();
        if (Project?.JobPosts != null)
        {
            foreach (var job in Project.JobPosts)
            {
                JobPosts.Add(new JobPostViewModel(job, LoadMoreRepliesAsync));
            }
        }
    }

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
       private async Task EditAnswersAsync(IJobPostDetails job)	{
		try
		{
			await Shell.Current.GoToAsync(nameof(JobWizardPage), new Dictionary<string, object>
			{
				{ "ProjectId", job.Project.Id },
				{ "JobPostId", job.Id },
				{ "TargetCategoryId", job.ServiceCategory.Id }
			});
		}
		catch (Exception ex)
		{
			await Shell.Current.DisplayAlert("Navigation Error", ex.Message, "OK");
		}
	}

	[RelayCommand]
	private async Task ReviewScopeAsync(IJobPostDetails job)
	{
		try
		{
			await Shell.Current.GoToAsync(nameof(ScopeReviewPage), new Dictionary<string, object>
			{
				{ "Job", job }
			});
		}
		catch (Exception ex)
		{
			await Shell.Current.DisplayAlert("Navigation Error", ex.Message, "OK");
		}
	}

	[RelayCommand]
	private async Task RespondToAdminAsync(IJobPostDetails job)
	{
	        string response = await Shell.Current.DisplayPromptAsync("Respond to Admin", $"Provide clarification for '{job.Title}':", "Send", "Cancel", "Write your response...");
	        if (string.IsNullOrWhiteSpace(response)) return;

	        try
	        {
	                IsBusy = true;
	                var result = await _apiClient.AddJobFeedback.ExecuteAsync(job.Id, response);

	                if (result.Errors.Count > 0)
	                {
	                        await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
	                        return;
	                }

	                await ReloadProjectAsync();
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
    private async Task EditFeedbackAsync(IFeedbackDetails feedback)
    {
        if (feedback == null) return;

        string newText = await Shell.Current.DisplayPromptAsync("Edit Comment", "Update your comment:", "Save", "Cancel", initialValue: feedback.Text);
        if (string.IsNullOrWhiteSpace(newText) || newText == feedback.Text) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.EditJobFeedback.ExecuteAsync(feedback.Id, newText);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            await ReloadProjectAsync();
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
    private async Task ReplyToFeedbackAsync(IFeedbackDetails feedback)
    {
        if (feedback == null) return;

        string replyText = await Shell.Current.DisplayPromptAsync("Reply to Feedback", "Type your reply:", "Send", "Cancel", "...");
        if (string.IsNullOrWhiteSpace(replyText)) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.ReplyToJobFeedback.ExecuteAsync(feedback.Id, replyText);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            await ReloadProjectAsync();
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

	                string replyText = await Shell.Current.DisplayPromptAsync("Reply", "Type your reply:", "Send", "Cancel", "...");
	                if (string.IsNullOrWhiteSpace(replyText)) return;

	                try
	                {
	                IsBusy = true;

	                var result = await _apiClient.ReplyToJobQuestion.ExecuteAsync(question.Id, replyText);

	                if (result.Errors.Count > 0)
	                {
	                        await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
	                        return;
	                }

	                if (Project != null)
	                {
	                        await ReloadProjectAsync();
	                }	        }
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
    private async Task ReplyToNestedQuestionAsync(IQuestionReplyDetails reply)
    {
        if (reply == null) return;
        var parentId = reply.ParentQuestionId;
        if (!parentId.HasValue) return;

        string replyText = await Shell.Current.DisplayPromptAsync("Reply", "Type your reply:", "Send", "Cancel", "...");
        if (string.IsNullOrWhiteSpace(replyText)) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.ReplyToJobQuestion.ExecuteAsync(parentId.Value, replyText);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            await ReloadProjectAsync();
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
    private async Task ReplyToNestedFeedbackAsync(IFeedbackReplyDetails reply)
    {
        if (reply == null) return;
        
        var parentId = reply.ParentFeedbackId;
        if (!parentId.HasValue) return;

        string replyText = await Shell.Current.DisplayPromptAsync("Reply to Feedback", "Type your reply:", "Send", "Cancel", "...");
        if (string.IsNullOrWhiteSpace(replyText)) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.ReplyToJobFeedback.ExecuteAsync(parentId.Value, replyText);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            await ReloadProjectAsync();
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
	private async Task AnswerQuestionAsync(IQuestionDetails question)	{
		string answer = await Shell.Current.DisplayPromptAsync("Answer Tradesman", question.QuestionText, "Submit", "Cancel", "Write your answer here...");
		if (string.IsNullOrWhiteSpace(answer)) return;

		try
		{
			IsBusy = true;
			var result = await _apiClient.AnswerJobQuestion.ExecuteAsync(question.Id, answer);

			if (result.Errors.Count > 0)
			{
				await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
				return;
			}

			await Shell.Current.DisplayAlert("Success", "Answer posted.", "OK");
			await ReloadProjectAsync();
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

            await Shell.Current.DisplayAlert("Success", "Answer updated.", "OK");
            await ReloadProjectAsync();
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
    private async Task EditFeedbackReplyAsync(IFeedbackReplyDetails reply)
    {
        if (reply == null) return;

        string newText = await Shell.Current.DisplayPromptAsync("Edit Reply", "Update your reply:", "Save", "Cancel", initialValue: reply.Text);
        if (string.IsNullOrWhiteSpace(newText) || newText == reply.Text) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.EditJobFeedback.ExecuteAsync(reply.Id, newText);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            await Shell.Current.DisplayAlert("Success", "Feedback updated.", "OK");
            await ReloadProjectAsync();
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

            await Shell.Current.DisplayAlert("Success", "Reply updated.", "OK");
            await ReloadProjectAsync();
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
