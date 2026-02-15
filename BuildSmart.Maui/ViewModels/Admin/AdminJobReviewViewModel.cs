using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.Json.Nodes;

namespace BuildSmart.Maui.ViewModels.Admin;

public partial class QAPair : ObservableObject
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

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
    private ObservableCollection<QAPair> _currentJobDetails = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private IGetJobsForReview_JobPostsForReview? _selectedJob;

    [ObservableProperty]
    private bool _isDetailsVisible;

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
    private void ViewJobDetails(IGetJobsForReview_JobPostsForReview? job)
    {
        SelectedJob = job;
        CurrentJobDetails.Clear();
        IsDetailsVisible = false;

        if (job == null) return;
        try
        {
            if (string.IsNullOrEmpty(job.JobDetails) || string.IsNullOrEmpty(job.ServiceCategory.TemplateStructure)) return;

            var answers = JsonNode.Parse(job.JobDetails);
            var template = JsonNode.Parse(job.ServiceCategory.TemplateStructure);

            if (template?["questions"] is JsonArray qArray)
            {
                foreach (var qNode in qArray)
                {
                    var id = qNode?["id"]?.GetValue<string>();
                    var text = qNode?["text"]?.GetValue<string>();

                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(text))
                    {
                        var answer = answers?[id]?.GetValue<string>() ?? "(No Answer)";
                        CurrentJobDetails.Add(new QAPair { Question = text, Answer = answer });
                    }
                }
            }
            IsDetailsVisible = CurrentJobDetails.Any();
        }
        catch (Exception ex)
        {
            Shell.Current.DisplayAlert("Parsing Error", ex.Message, "OK");
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
        string feedback = await Shell.Current.DisplayPromptAsync("Reject Job", "Please provide a reason for rejection:", "Reject", "Cancel", "Reason...");
        if (string.IsNullOrWhiteSpace(feedback)) return;

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

            await Shell.Current.DisplayAlert("Success", approved ? "Job published to tradesmen." : "Job rejected and feedback sent.", "OK");
            await LoadJobsAsync();
            CurrentJobDetails.Clear(); // Clear details after action
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
