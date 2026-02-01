using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using BuildSmart.Maui.Views;

namespace BuildSmart.Maui.ViewModels;

public partial class MyProjectsViewModel : ObservableObject
{
    private readonly IBuildSmartApiClient _apiClient;
    private bool _isFirstLoad = true;

    [ObservableProperty]
    private ObservableCollection<IGetMyProjects_MyProjects> _projects = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isEmpty;

    public MyProjectsViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [RelayCommand]
    private async Task CreateProjectAsync()
    {
        await Shell.Current.GoToAsync(nameof(JobWizardPage));
    }

    [RelayCommand]
    public async Task LoadProjectsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.GetMyProjects.ExecuteAsync();

            if (result.Errors.Count > 0)
            {
                var error = result.Errors.First();
                await Shell.Current.DisplayAlert("GraphQL Error", $"{error.Message}\nCode: {error.Code}", "OK");
                return;
            }

            Projects.Clear();
            if (result.Data?.MyProjects != null)
            {
                var sortedProjects = result.Data.MyProjects.OrderByDescending(p => p.CreatedAt).ToList();
                foreach (var project in sortedProjects)
                {
                    Projects.Add(project);
                }

                IsEmpty = !Projects.Any();

                // Auto-navigation removed as per user request to see list first
            }
            else 
            {
                 IsEmpty = true;
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Unexpected error: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoToDetails(IGetMyProjects_MyProjects project)
    {
            await Shell.Current.GoToAsync(nameof(ProjectDetailPage), new Dictionary<string, object>
            {
                { "Project", project }
            });
    }

    [RelayCommand]
    private async Task DeleteProjectAsync(IGetMyProjects_MyProjects project)
    {
        if (project == null) return;

        bool confirm = await Shell.Current.DisplayAlert("Delete Project", $"Are you sure you want to delete '{project.Title}'?", "Yes", "No");
        if (!confirm) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.DeleteProject.ExecuteAsync(project.Id);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors.First().Message, "OK");
                return;
            }

            if (result.Data?.DeleteProject == true)
            {
                Projects.Remove(project);
                IsEmpty = !Projects.Any();
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to delete project.", "OK");
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