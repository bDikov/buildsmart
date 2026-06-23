using BuildSmart.SharedUI.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using BuildSmart.SharedUI.Services;

namespace BuildSmart.SharedUI.ViewModels;

public partial class MyProjectsViewModel : ObservableObject, IDisposable
{
	private readonly IBuildSmartApiClient _apiClient;
	private readonly SignalRService _signalRService;
	private readonly IAuthService _authService;
	private bool _isFirstLoad = true;

	[ObservableProperty]
	private ObservableCollection<IProjectDetails> _projects = new();

	[ObservableProperty]
	private bool _isBusy;

	[ObservableProperty]
	private bool _isEmpty;

	[ObservableProperty]
	private bool _isAdmin;

	[ObservableProperty]
	private string _searchQuery = string.Empty;

	[ObservableProperty]
	private string _filterUserId = string.Empty;

	[ObservableProperty]
	private string _filterUserEmail = string.Empty;

	[ObservableProperty]
	private string _filterStatus = "ALL";

	public MyProjectsViewModel(IBuildSmartApiClient apiClient, SignalRService signalRService, IAuthService authService)
	{
		_apiClient = apiClient;
		_signalRService = signalRService;
		_authService = authService;

		// Subscribe to notifications
		_signalRService.NotificationReceived += OnNotificationReceived;
	}

	private void OnNotificationReceived(string title, string message, object? data)
	{
		// Reload projects when ANY notification is received (simple approach)
		AppServiceLocator.MainThread.BeginInvokeOnMainThread(async () => await LoadProjectsAsync());
	}

	[RelayCommand]
	private async Task CreateProjectAsync()
	{
		await AppServiceLocator.Navigation.NavigateToAsync("/job-wizard");
	}

	[RelayCommand]
	public async Task LoadProjectsAsync()
	{
		if (IsBusy) return;

		try
		{
			IsBusy = true;

			// Check admin role
			var token = await _authService.GetTokenAsync();
			var role = _authService.GetUserRoleFromToken(token);
			IsAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) || 
			          string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase);

			Projects.Clear();

			if (IsAdmin)
			{
				ProjectFilterInput? filter = null;
				var andConditions = new List<ProjectFilterInput>();

				// Search Query (title, description, email, names)
				if (!string.IsNullOrWhiteSpace(SearchQuery))
				{
					var textFilter = new StringOperationFilterInput { Contains = SearchQuery };
					var orConditions = new List<ProjectFilterInput>
					{
						new ProjectFilterInput { Title = textFilter },
						new ProjectFilterInput { Description = textFilter },
						new ProjectFilterInput { Homeowner = new UserFilterInput { Email = textFilter } },
						new ProjectFilterInput { Homeowner = new UserFilterInput { FirstName = textFilter } },
						new ProjectFilterInput { Homeowner = new UserFilterInput { LastName = textFilter } }
					};

					if (Guid.TryParse(SearchQuery, out var guid))
					{
						orConditions.Add(new ProjectFilterInput { Id = new UuidOperationFilterInput { Eq = guid } });
						orConditions.Add(new ProjectFilterInput { HomeownerId = new UuidOperationFilterInput { Eq = guid } });
					}

					andConditions.Add(new ProjectFilterInput { Or = orConditions });
				}

				// User ID filter
				if (!string.IsNullOrWhiteSpace(FilterUserId) && Guid.TryParse(FilterUserId, out var filterGuid))
				{
					andConditions.Add(new ProjectFilterInput { HomeownerId = new UuidOperationFilterInput { Eq = filterGuid } });
				}

				// User Email filter
				if (!string.IsNullOrWhiteSpace(FilterUserEmail))
				{
					andConditions.Add(new ProjectFilterInput 
					{ 
						Homeowner = new UserFilterInput 
						{ 
							Email = new StringOperationFilterInput { Eq = FilterUserEmail.Trim() } 
						} 
					});
				}

				// Status filter
				if (!string.IsNullOrWhiteSpace(FilterStatus) && !string.Equals(FilterStatus, "ALL", StringComparison.OrdinalIgnoreCase))
				{
					if (Enum.TryParse<ProjectStatus>(FilterStatus, true, out var statusEnum))
					{
						andConditions.Add(new ProjectFilterInput { Status = new ProjectStatusOperationFilterInput { Eq = statusEnum } });
					}
				}

				if (andConditions.Any())
				{
					filter = new ProjectFilterInput { And = andConditions };
				}

				var order = new List<ProjectSortInput>
				{
					new ProjectSortInput { CreatedAt = SortEnumType.Desc }
				};

				var result = await _apiClient.GetAllProjects.ExecuteAsync(filter, order);
				if (result.Errors.Count > 0)
				{
					var error = result.Errors.First();
					await AppServiceLocator.Alerts.DisplayAlert("GraphQL Error", $"{error.Message}", "OK");
					return;
				}

				if (result.Data?.AllProjects != null)
				{
					foreach (var project in result.Data.AllProjects)
					{
						Projects.Add(project);
					}
				}
			}
			else
			{
				// Regular user
				var result = await _apiClient.GetMyProjects.ExecuteAsync();

				if (result.Errors.Count > 0)
				{
					var error = result.Errors.First();
					await AppServiceLocator.Alerts.DisplayAlert("GraphQL Error", $"{error.Message}", "OK");
					return;
				}

				if (result.Data?.MyProjects != null)
				{
					var sortedProjects = result.Data.MyProjects.OrderByDescending(p => p.CreatedAt).ToList();
					foreach (var project in sortedProjects)
					{
						Projects.Add(project);
					}
				}
			}

			IsEmpty = !Projects.Any();
		}
		catch (Exception ex)
		{
			await AppServiceLocator.Alerts.DisplayAlert("Error", $"Unexpected error: {ex.Message}", "OK");
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private async Task GoToDetails(IProjectDetails project)
	{
		if (project.Status == ProjectStatus.Draft)
		{
			await AppServiceLocator.Navigation.NavigateToAsync($"/job-wizard?ProjectId={project.Id}");
		}
		else if (!project.HasOfferPdf)
		{
			await AppServiceLocator.Alerts.DisplayAlert("Processing", "Your project is currently being processed by the AI. You will be notified when the offer is ready.", "OK");
		}
		else
		{
			await AppServiceLocator.Navigation.NavigateToAsync($"/project-detail?ProjectId={project.Id}");
		}
	}

	[RelayCommand]
	private async Task DeleteProjectAsync(IProjectDetails project)
	{
		if (project == null) return;

		bool confirm = await AppServiceLocator.Alerts.DisplayAlert("Delete Project", $"Are you sure you want to delete '{project.Title}'?", "Yes", "No");
		if (!confirm) return;

		try
		{
			IsBusy = true;
			var result = await _apiClient.DeleteProject.ExecuteAsync(project.Id);

			if (result.Errors.Count > 0)
			{
				await AppServiceLocator.Alerts.DisplayAlert("Error", result.Errors.First().Message, "OK");
				return;
			}

			if (result.Data?.DeleteProject == true)
			{
				Projects.Remove(project);
				IsEmpty = !Projects.Any();
			}
			else
			{
				await AppServiceLocator.Alerts.DisplayAlert("Error", "Failed to delete project.", "OK");
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

	public void Dispose()
	{
		_signalRService.NotificationReceived -= OnNotificationReceived;
	}
}





