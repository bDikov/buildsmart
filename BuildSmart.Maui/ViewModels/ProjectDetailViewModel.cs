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

	public ProjectDetailViewModel(IBuildSmartApiClient apiClient, SignalRService signalRService)
	{
		_apiClient = apiClient;
		_signalRService = signalRService;

		_signalRService.NotificationReceived += OnNotificationReceived;
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

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("Project", out var projectObj) && projectObj is IGetMyProjects_MyProjects project)
		{
			Project = project;
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

			await Shell.Current.DisplayAlert("Success", "Response sent to Admin.", "OK");
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