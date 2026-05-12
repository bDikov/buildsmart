using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.MauiMocks;
using BuildSmart.SharedUI.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.SharedUI.ViewModels;

public partial class TaskBreakdownViewModel : ObservableObject, IQueryAttributable
{
    private readonly IBuildSmartApiClient _apiClient;

    public TaskBreakdownViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
        Tasks = new ObservableCollection<IGetJobTasks_AllJobPosts_JobTasks>();
    }

    [ObservableProperty]
    private IJobPostDetails? _job;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<IGetJobTasks_AllJobPosts_JobTasks> Tasks { get; }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Job", out var jobObj) && jobObj is IJobPostDetails job)
        {
            Job = job;
            await LoadTasksAsync(job.Id);
        }
        else if (query.TryGetValue("JobId", out var jobIdObj) && Guid.TryParse(jobIdObj.ToString(), out var jobId))
        {
            await LoadTasksAsync(jobId);
        }
    }

    private async Task LoadTasksAsync(Guid jobId)
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            Tasks.Clear();

            var result = await _apiClient.GetJobTasks.ExecuteAsync(jobId);
            
            var jobPost = result.Data?.AllJobPosts?.FirstOrDefault();
            if (jobPost?.JobTasks != null)
            {
                foreach (var task in jobPost.JobTasks.OrderBy(t => t.SequenceOrder))
                {
                    Tasks.Add(task);
                }
            }
        }
        catch (Exception ex)
        {
            await AppServiceLocator.Alerts.DisplayAlert("Error", "Failed to load tasks: " + ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        await AppServiceLocator.Navigation.NavigateToAsync("..");
    }
}





