using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BuildSmart.Maui.Views;

namespace BuildSmart.Maui.ViewModels;

public partial class ProjectDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IBuildSmartApiClient _apiClient;

    public ProjectDetailViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
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
    private async Task SubmitForGenerationAsync(IGetMyProjects_MyProjects_JobPosts job)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.SubmitJobForScopeGeneration.ExecuteAsync(job.Id);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            await Shell.Current.DisplayAlert("Success", "AI is now generating your scope. Please refresh in a few seconds.", "OK");
            // Optionally trigger a refresh command here if available
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
    private async Task ReviewScopeAsync(IGetMyProjects_MyProjects_JobPosts job)
    {
        await Shell.Current.GoToAsync("ScopeReviewPage", new Dictionary<string, object>
        {
            { "Job", job }
        });
    }
}
