using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuildSmart.Maui.ViewModels;

public partial class ScopeReviewViewModel : ObservableObject, IQueryAttributable
{
    private readonly IBuildSmartApiClient _apiClient;

    public ScopeReviewViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [ObservableProperty]
    private IGetMyProjects_MyProjects_JobPosts? _job;

    [ObservableProperty]
    private string _editableScope = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Job", out var jobObj) && jobObj is IGetMyProjects_MyProjects_JobPosts job)
        {
            Job = job;
            EditableScope = job.GeneratedScope ?? string.Empty;
        }
    }

    [RelayCommand]
    private async Task ApproveAsync()
    {
        if (IsBusy || Job == null) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.ApproveJobScope.ExecuteAsync(Job.Id, EditableScope);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            await Shell.Current.DisplayAlert("Success", "Scope approved and sent to Admin for final check.", "OK");
            await Shell.Current.GoToAsync("..");
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
