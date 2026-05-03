using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.MauiMocks;
using BuildSmart.SharedUI.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace BuildSmart.SharedUI.ViewModels;

public partial class EditableCriteriaViewModel : ObservableObject
{
    public Guid? Id { get; set; }

    [ObservableProperty]
    private string _description = string.Empty;
}

public partial class EditableTaskViewModel : ObservableObject
{
    public EditableTaskViewModel()
    {
        Criteria = new ObservableCollection<EditableCriteriaViewModel>();
    }

    public Guid? Id { get; set; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private int _sequenceOrder;

    [ObservableProperty]
    private decimal _estimatedPrice;

    public ObservableCollection<EditableCriteriaViewModel> Criteria { get; }

    [RelayCommand]
    private void AddCriteria()
    {
        Criteria.Add(new EditableCriteriaViewModel { Description = "New Acceptance Criteria" });
    }

    [RelayCommand]
    private void RemoveCriteria(EditableCriteriaViewModel criteria)
    {
        if (criteria != null && Criteria.Contains(criteria))
        {
            Criteria.Remove(criteria);
        }
    }
}

public partial class ScopeReviewViewModel : ObservableObject, IQueryAttributable
{
    private readonly IBuildSmartApiClient _apiClient;

    public ScopeReviewViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
        Tasks = new ObservableCollection<EditableTaskViewModel>();
    }

    [ObservableProperty]
    private IJobPostDetails? _job;

    private Guid _jobId;

    [ObservableProperty]
    private string _generatedScope = string.Empty;

    public ObservableCollection<EditableTaskViewModel> Tasks { get; }

    [ObservableProperty]
    private bool _isBusy;

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("JobId", out var jobIdObj) && Guid.TryParse(jobIdObj.ToString(), out var jobId))
        {
            _jobId = jobId;
            Tasks.Clear();
            IsBusy = true;

            try
            {
                var result = await _apiClient.GetJobTasks.ExecuteAsync(jobId);
                var jobPost = result.Data?.AllJobPosts?.FirstOrDefault();

                if (jobPost != null)
                {
                    GeneratedScope = jobPost.GeneratedScope ?? string.Empty;

                    if (jobPost.JobTasks != null && jobPost.JobTasks.Any())
                    {
                        foreach (var task in jobPost.JobTasks.OrderBy(t => t.SequenceOrder))
                        {
                            var vm = new EditableTaskViewModel
                            {
                                Id = task.Id,
                                Title = task.Title,
                                Description = task.Description ?? string.Empty,
                                SequenceOrder = task.SequenceOrder,
                                EstimatedPrice = task.EstimatedPrice
                            };

                            if (task.AcceptanceCriteria != null)
                            {
                                foreach (var criteria in task.AcceptanceCriteria)
                                {
                                    vm.Criteria.Add(new EditableCriteriaViewModel 
                                    { 
                                        Id = criteria.Id,
                                        Description = criteria.Description 
                                    });
                                }
                            }

                            Tasks.Add(vm);
                        }
                    }
                }
            }
            catch { /* Silently fail on lazy load */ }
            finally
            {
                IsBusy = false;
            }
            
            // If no tasks exist, start fresh with one empty task
            if (Tasks.Count == 0)
            {
                AddTask();
            }
        }
    }

    [RelayCommand]
    private void AddTask()
    {
        var newTask = new EditableTaskViewModel
        {
            Title = "New Task",
            SequenceOrder = Tasks.Count + 1
        };
        newTask.AddCriteriaCommand.Execute(null); // Add one empty criteria by default
        Tasks.Add(newTask);
    }

    [RelayCommand]
    private void RemoveTask(EditableTaskViewModel task)
    {
        if (task != null && Tasks.Contains(task))
        {
            Tasks.Remove(task);
            // Re-sequence
            for (int i = 0; i < Tasks.Count; i++)
            {
                Tasks[i].SequenceOrder = i + 1;
            }
        }
    }

    [RelayCommand]
    private async Task BackToEditAnswersAsync()
    {
        await AppServiceLocator.Navigation.NavigateToAsync("..");
    }

    [RelayCommand]
    private async Task ApproveAsync()
    {
        if (_jobId == Guid.Empty)
        {
            await AppServiceLocator.Alerts.DisplayAlert("Error", "Job data is missing. Please go back and try again.", "OK");
            return;
        }

        if (Tasks.Count == 0)
        {
            await AppServiceLocator.Alerts.DisplayAlert("Validation", "You must create at least one task before submitting.", "OK");
            return;
        }

        foreach (var task in Tasks)
        {
            if (string.IsNullOrWhiteSpace(task.Title))
            {
                await AppServiceLocator.Alerts.DisplayAlert("Validation", "All tasks must have a title.", "OK");
                return;
            }
            if (task.Criteria.Count == 0)
            {
                await AppServiceLocator.Alerts.DisplayAlert("Validation", $"Task '{task.Title}' must have at least one acceptance criterion.", "OK");
                return;
            }
            if (task.Criteria.Any(c => string.IsNullOrWhiteSpace(c.Description)))
            {
                await AppServiceLocator.Alerts.DisplayAlert("Validation", $"Task '{task.Title}' has empty acceptance criteria. Please fill them out or remove them.", "OK");
                return;
            }
        }

        if (IsBusy) return;

        try
        {
            IsBusy = true;
            Console.WriteLine($"[ScopeReview] Sending UpdateJobTasks mutation for Job: {_jobId}...");

            // Map UI models to GraphQL Input
            var taskInputs = Tasks.Select(t => new JobTaskInput
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description ?? string.Empty,
                SequenceOrder = t.SequenceOrder,
                Criteria = t.Criteria.Select(c => new TaskAcceptanceCriteriaInput
                {
                    Id = c.Id,
                    Description = c.Description
                }).ToList()
            }).ToList();

            var updateInput = new UpdateJobTasksInput
            {
                JobPostId = _jobId,
                Tasks = taskInputs
            };

            // 1. Submit the detailed Tasks breakdown
            var tasksResult = await _apiClient.UpdateJobTasks.ExecuteAsync(updateInput);
            if (tasksResult.Errors.Count > 0)
            {
                await AppServiceLocator.Alerts.DisplayAlert("Error Saving Tasks", tasksResult.Errors[0].Message, "OK");
                return;
            }

            // 2. Navigate to the OfferView instead of approving
            await AppServiceLocator.Navigation.NavigateToAsync($"GeneratedOfferPage?jobId={_jobId}");
        }
        catch (Exception ex)
        {
            // Handle common transition errors gracefully
            if (ex.Message.Contains("WaitingForAdminReview") || ex.Message.Contains("status OPEN"))
            {
                await AppServiceLocator.Navigation.NavigateToAsync("//BlazorHostPage");
            }
            else
            {
                await AppServiceLocator.Alerts.DisplayAlert("System Error", ex.Message, "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}





