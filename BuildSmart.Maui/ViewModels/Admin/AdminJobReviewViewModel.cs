using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using BuildSmart.Maui.Services;

namespace BuildSmart.Maui.ViewModels.Admin;

public partial class QAPair : ObservableObject
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

public partial class AdminJobReviewViewModel : ObservableObject
{
    private readonly IBuildSmartApiClient _apiClient;
    private readonly SignalRService _signalRService;

    public AdminJobReviewViewModel(IBuildSmartApiClient apiClient, SignalRService signalRService)
    {
        _apiClient = apiClient;
        _signalRService = signalRService;

        _signalRService.NotificationReceived += OnNotificationReceived;
    }

    private void OnNotificationReceived(string title, string message)
    {
        MainThread.BeginInvokeOnMainThread(async () => {
            await LoadJobsAsync();
            
            // If a job is selected, try to refresh its details too
            if (SelectedJob != null)
            {
                var updatedProject = Projects.FirstOrDefault(p => p.JobPosts.Any(j => j.Id == SelectedJob.Id));
                var updatedJob = updatedProject?.JobPosts.FirstOrDefault(j => j.Id == SelectedJob.Id);
                if (updatedJob != null) await ViewJobDetails(updatedJob);
            }
        });
    }

    [ObservableProperty]
    private ObservableCollection<IGetProjectsForReview_ProjectsForReview> _projects = new();

    [ObservableProperty]
    private ObservableCollection<QAPair> _currentJobDetails = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private IGetProjectsForReview_ProjectsForReview_JobPosts? _selectedJob;

    [ObservableProperty]
    private bool _isDetailsVisible;

    [ObservableProperty]
    private string _newFeedbackText = string.Empty;

    [RelayCommand]
    public async Task LoadJobsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.GetProjectsForReview.ExecuteAsync();

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            Projects.Clear();
            if (result.Data?.ProjectsForReview != null)
            {
                foreach (var project in result.Data.ProjectsForReview)
                {
                    Projects.Add(project);
                }
            }
            
            IsEmpty = !Projects.Any();
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
            private async Task ViewJobDetails(IGetProjectsForReview_ProjectsForReview_JobPosts? job)
            {
                SelectedJob = job;
                CurrentJobDetails.Clear();
                
                if (job == null)
                {
                    IsDetailsVisible = false;
                    return;
                }
        
                IsDetailsVisible = true;
        
                // Add Metadata
                CurrentJobDetails.Add(new QAPair { Question = "Job Location", Answer = job.Location ?? "Not Specified" });
                if (job.EstimatedBudget != null)
                {
                    CurrentJobDetails.Add(new QAPair { Question = "Estimated Budget", Answer = $"{job.EstimatedBudget.Total} {job.EstimatedBudget.Currency}" });
                }
        
                try
                {
                    if (!string.IsNullOrEmpty(job.JobDetails))
                    {
                        var answers = JsonNode.Parse(job.JobDetails);
                        
                        // Fetch ALL active categories to find matching questions (including Global ones)
                        var categoriesResult = await _apiClient.GetServiceCategories.ExecuteAsync();
                        var allRelevantCategories = new List<string>();
                        
                        if (categoriesResult.Data?.ServiceCategories != null)
                        {
                            foreach (var cat in categoriesResult.Data.ServiceCategories)
                            {
                                // Match if it's the job's category OR if it's a Global category
                                if (cat.Name == job.ServiceCategory.Name || cat.IsGlobal)
                                {
                                    if (!string.IsNullOrEmpty(cat.TemplateStructure))
                                    {
                                        var template = JsonNode.Parse(cat.TemplateStructure);
                                        if (template?["questions"] is JsonArray qArray)
                                        {
                                            foreach (var qNode in qArray)
                                            {
                                                var id = qNode?["id"]?.GetValue<string>();
                                                var text = qNode?["text"]?.GetValue<string>();
        
                                                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(text))
                                                {
                                                    var answer = answers?[id]?.GetValue<string>();
                                                    if (!string.IsNullOrEmpty(answer))
                                                    {
                                                        CurrentJobDetails.Add(new QAPair { Question = text, Answer = answer });
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
        
                        // If after checking categories we have no questions (or if parsing failed), 
                        // show raw data so nothing is hidden.
                        if (CurrentJobDetails.Count <= 2 && answers is JsonObject obj)
                        {
                            foreach (var kvp in obj)
                            {
                                // Skip keys that might have been added by metadata
                                if (kvp.Key == "location" || kvp.Key == "budget") continue;
                                CurrentJobDetails.Add(new QAPair { Question = kvp.Key, Answer = kvp.Value?.ToString() ?? "(Empty)" });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Parsing Error: {ex.Message}");
                }
            }    [RelayCommand]
    private void CloseDetails()
    {
        IsDetailsVisible = false;
        SelectedJob = null;
    }

    [RelayCommand]
    private async Task ApproveJobAsync(IGetProjectsForReview_ProjectsForReview_JobPosts? job)
    {
        if (job == null) return;
        bool confirm = await Shell.Current.DisplayAlert("Confirm", $"Approve scope for '{job.Title}'?", "Yes", "No");
        if (!confirm) return;

        await PerformReview(job.Id, true, null);
    }

    [RelayCommand]
    private async Task RejectJobAsync(IGetProjectsForReview_ProjectsForReview_JobPosts? job)
    {
        if (job == null) return;
        string feedback = await Shell.Current.DisplayPromptAsync("Reject Job", "Please provide a reason for rejection:", "Reject", "Cancel", "Reason...");
        if (string.IsNullOrWhiteSpace(feedback)) return;

        await PerformReview(job.Id, false, feedback);
    }

    [RelayCommand]
    private async Task AddFeedbackAsync()
    {
        if (SelectedJob == null || string.IsNullOrWhiteSpace(NewFeedbackText)) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.AddJobFeedback.ExecuteAsync(SelectedJob.Id, NewFeedbackText);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            NewFeedbackText = string.Empty;
            await LoadJobsAsync(); // Reload to show new feedback
            // Re-select job to refresh panel
            var updatedProject = Projects.FirstOrDefault(p => p.JobPosts.Any(j => j.Id == SelectedJob.Id));
            var updatedJob = updatedProject?.JobPosts.FirstOrDefault(j => j.Id == SelectedJob.Id);
            if (updatedJob != null) ViewJobDetails(updatedJob);
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
    private async Task ResolveFeedbackAsync(Guid feedbackId)
    {
        try
        {
            IsBusy = true;
            var result = await _apiClient.ResolveJobFeedback.ExecuteAsync(feedbackId);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            await LoadJobsAsync();
            var updatedProject = Projects.FirstOrDefault(p => p.JobPosts.Any(j => j.Feedbacks.Any(f => f.Id == feedbackId)));
            var updatedJob = updatedProject?.JobPosts.FirstOrDefault(j => j.Feedbacks.Any(f => f.Id == feedbackId));
            if (updatedJob != null) ViewJobDetails(updatedJob);
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
            
            // Fix: Close details and refresh list
            CloseDetails();
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
