using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.Maui.ViewModels;

public partial class GeneratedOfferViewModel : ObservableObject, IQueryAttributable
{
    private readonly IBuildSmartApiClient _apiClient;

    public GeneratedOfferViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [ObservableProperty]
    private IJobPostDetails? _job;

    [ObservableProperty]
    private decimal _totalEstimatedPrice;

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<IJobTaskDetails> Tasks { get; } = new();

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Job", out var jobObj) && jobObj is IJobPostDetails job)
        {
            Job = job;
            await LoadOfferDetailsAsync(job.Id);
        }
    }

    private async Task LoadOfferDetailsAsync(Guid jobId)
    {
        try
        {
            IsBusy = true;
            var result = await _apiClient.GetJobTasks.ExecuteAsync(jobId);
            var jobPost = result.Data?.AllJobPosts?.FirstOrDefault();

            Tasks.Clear();
            decimal total = 0;

            if (jobPost?.JobTasks != null)
            {
                foreach (var task in jobPost.JobTasks.OrderBy(t => t.SequenceOrder))
                {
                    Tasks.Add(task);
                    total += task.EstimatedPrice;
                }
            }

            TotalEstimatedPrice = total;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", "Could not load offer details: " + ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SubmitToAdminAsync()
    {
        if (Job == null) return;
        
        try
        {
            IsBusy = true;
            var approveResult = await _apiClient.ApproveJobScope.ExecuteAsync(Job.Id, string.Empty);

            if (approveResult.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", approveResult.Errors[0].Message, "OK");
                return;
            }

            await Shell.Current.DisplayAlert("Success", "Your offer has been submitted to the Admin for final review.", "OK");
            await Shell.Current.GoToAsync("//BlazorHostPage");
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
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}