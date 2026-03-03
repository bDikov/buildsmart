using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BuildSmart.Maui.Views;
using BuildSmart.Maui.Services;

namespace BuildSmart.Maui.ViewModels;

public partial class ProjectDetailViewModel : ObservableObject, IQueryAttributable
{
	private readonly IBuildSmartApiClient _apiClient;
	private readonly SignalRService _signalRService;
    private readonly IAuthService _authService;

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
                var role = _authService.GetUserRoleFromToken(token);
                IsHomeowner = string.Equals(role, "HOMEOWNER", StringComparison.OrdinalIgnoreCase) || 
                              string.Equals(role, "Homeowner", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch { /* Silently fail, default to false */ }
    }

	private void OnNotificationReceived(string title, string message, object? data)
	{
		// Reload project if we are currently viewing one
		if (Project != null)
		{
			MainThread.BeginInvokeOnMainThread(async () => await ReloadProjectAsync());
		}
	}

	private async Task ReloadProjectAsync()
	{
		if (Project == null) return;

		try
		{
			var result = await _apiClient.GetMyProjects.ExecuteAsync();
			if (result.Data?.MyProjects != null)
			{
				var updated = result.Data.MyProjects.FirstOrDefault(p => p.Id == Project.Id);
				if (updated != null)
				{
					Project = updated;
				}
			}
		}
		catch { /* Silently fail reload */ }
	}

	[ObservableProperty]
	private IGetMyProjects_MyProjects? _project;

	[ObservableProperty]
	private bool _isBusy;

    [ObservableProperty]
    private bool _isHomeowner;

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("Project", out var projectObj))
		{
            if (projectObj is IGetMyProjects_MyProjects project)
            {
			    Project = project;
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(async () => 
                    await Shell.Current.DisplayAlert("Debug", $"Found Project key, but type is {projectObj?.GetType().Name} instead of IGetMyProjects_MyProjects", "OK"));
            }
		}
        else
        {
            MainThread.BeginInvokeOnMainThread(async () => 
                    await Shell.Current.DisplayAlert("Debug", "No 'Project' key found in query attributes.", "OK"));
        }
	}

	[RelayCommand]
	private async Task EditAnswersAsync(IGetMyProjects_MyProjects_JobPosts job)
	{
		try
		{
			// Navigate to Wizard in Edit mode for a SPECIFIC job/category
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
	private async Task ReviewScopeAsync(IGetMyProjects_MyProjects_JobPosts job)
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
	private async Task RespondToAdminAsync(IGetMyProjects_MyProjects_JobPosts job)
	{
	        string response = await Shell.Current.DisplayPromptAsync("Respond to Admin", $"Provide clarification for '{job.Title}':", "Send", "Cancel", "Write your response...");
	        if (string.IsNullOrWhiteSpace(response)) return;

	        try
	        {
	                IsBusy = true;
	                // Note: AddJobFeedback mutation might need to be imported or available in this context     
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
    private async Task ReplyToFeedbackAsync(IGetMyProjects_MyProjects_JobPosts_Feedbacks feedback)
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
	                private async Task ReplyToQuestionAsync(IGetMyProjects_MyProjects_JobPosts_Questions question)
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
    private async Task ReplyToNestedQuestionAsync(IGetMyProjects_MyProjects_JobPosts_Questions_Replies reply)
    {
        if (reply == null) return;
        // Map to top-level question for simple 1-level threading
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
    private async Task ReplyToNestedFeedbackAsync(IGetMyProjects_MyProjects_JobPosts_Feedbacks_Replies reply)
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
	private async Task AnswerQuestionAsync(IGetMyProjects_MyProjects_JobPosts_Questions question)	{
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
    private async Task EditAnswerAsync(IGetMyProjects_MyProjects_JobPosts_Questions question)
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
}
