using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.Maui.ViewModels.Admin;

public partial class AdminJobReviewViewModel : ObservableObject
{
    private readonly IBuildSmartApiClient _apiClient;

    public AdminJobReviewViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [ObservableProperty]
    private ObservableCollection<IGetJobsForReview_JobPostsForReview> _jobs = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isEmpty;

    [RelayCommand]
    public async Task LoadJobsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.GetJobsForReview.ExecuteAsync();

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            Jobs.Clear();
            if (result.Data?.JobPostsForReview != null)
            {
                foreach (var job in result.Data.JobPostsForReview)
                {
                    Jobs.Add(job);
                }
            }
            
            IsEmpty = !Jobs.Any();
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
    private async Task ApproveJobAsync(IGetJobsForReview_JobPostsForReview job)
    {
        bool confirm = await Shell.Current.DisplayAlert("Confirm", $"Approve scope for '{job.Title}'?", "Yes", "No");
        if (!confirm) return;

        await PerformReview(job.Id, true, null);
    }

    [RelayCommand]
    private async Task RejectJobAsync(IGetJobsForReview_JobPostsForReview job)
    {
        string feedback = await Shell.Current.DisplayActionSheet("Select Reason", "Cancel", null, "Incomplete info", "Vague description", "Other");
        if (feedback == "Cancel" || feedback == null) return;

        await PerformReview(job.Id, false, feedback);
    }

    private async Task PerformReview(Guid jobId, bool approved, string? feedback)
    {
        try
        {
            IsBusy = true;
            var result = await _apiClient.AdminReviewJobScope.ExecuteAsync(jobId, approved, feedback);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            await Shell.Current.DisplayAlert("Success", approved ? "Job published to tradesmen." : "Feedback sent to homeowner.", "OK");
            await LoadJobsAsync();
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
