using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace BuildSmart.Maui.ViewModels;

public partial class EditableCriteriaViewModel : ObservableObject
{
    [ObservableProperty]
    private string _description = string.Empty;
}

public partial class EditableTaskViewModel : ObservableObject
{
    public EditableTaskViewModel()
    {
        Criteria = new ObservableCollection<EditableCriteriaViewModel>();
    }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private int _sequenceOrder;

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

    [ObservableProperty]
    private string _generatedScope = string.Empty;

    public ObservableCollection<EditableTaskViewModel> Tasks { get; }

    [ObservableProperty]
    private bool _isBusy;

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Job", out var jobObj) && jobObj is IJobPostDetails job)
        {
            Job = job;
            GeneratedScope = job.GeneratedScope ?? string.Empty;
            
            Tasks.Clear();

            try
            {
                var result = await _apiClient.GetJobTasks.ExecuteAsync(job.Id);
                var jobPost = result.Data?.AllJobPosts?.FirstOrDefault();

                if (jobPost?.JobTasks != null && jobPost.JobTasks.Any())
                {
                    foreach (var task in jobPost.JobTasks.OrderBy(t => t.SequenceOrder))
                    {
                        var vm = new EditableTaskViewModel
                        {
                            Title = task.Title,
                            Description = task.Description ?? string.Empty,
                            SequenceOrder = task.SequenceOrder
                        };

                        if (task.AcceptanceCriteria != null)
                        {
                            foreach (var criteria in task.AcceptanceCriteria)
                            {
                                vm.Criteria.Add(new EditableCriteriaViewModel { Description = criteria.Description });
                            }
                        }

                        Tasks.Add(vm);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScopeReview] Failed to lazy load tasks: {ex.Message}");
            }
            
            // If no tasks exist, start fresh with one empty task
            if (Tasks.Count == 0)
            {
                AddTask();
            }
            
            Console.WriteLine($"[ScopeReview] SUCCESS: Job loaded with ID: {Job.Id}. Tasks loaded: {Tasks.Count}");
        }
        else
        {
            Console.WriteLine("[ScopeReview] ERROR: Received invalid Job object or ID.");
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
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task ApproveAsync()
    {
        if (Job == null || Job.Id == Guid.Empty)
        {
            await Shell.Current.DisplayAlert("Error", "Job data is missing. Please go back and try again.", "OK");
            return;
        }

        if (Tasks.Count == 0)
        {
            await Shell.Current.DisplayAlert("Validation", "You must create at least one task before submitting.", "OK");
            return;
        }

        foreach (var task in Tasks)
        {
            if (string.IsNullOrWhiteSpace(task.Title))
            {
                await Shell.Current.DisplayAlert("Validation", "All tasks must have a title.", "OK");
                return;
            }
            if (task.Criteria.Count == 0)
            {
                await Shell.Current.DisplayAlert("Validation", $"Task '{task.Title}' must have at least one acceptance criterion.", "OK");
                return;
            }
            if (task.Criteria.Any(c => string.IsNullOrWhiteSpace(c.Description)))
            {
                await Shell.Current.DisplayAlert("Validation", $"Task '{task.Title}' has empty acceptance criteria. Please fill them out or remove them.", "OK");
                return;
            }
        }

        if (IsBusy) return;

        try
        {
            IsBusy = true;
            Console.WriteLine($"[ScopeReview] Sending UpdateJobTasks mutation for Job: {Job.Id}...");

            // Map UI models to GraphQL Input
            var taskInputs = Tasks.Select(t => new JobTaskInput
            {
                Title = t.Title,
                Description = t.Description ?? string.Empty,
                SequenceOrder = t.SequenceOrder,
                Criteria = t.Criteria.Select(c => new TaskAcceptanceCriteriaInput
                {
                    Description = c.Description
                }).ToList()
            }).ToList();

            var updateInput = new UpdateJobTasksInput
            {
                JobPostId = Job.Id,
                Tasks = taskInputs
            };

            // 1. Submit the detailed Tasks breakdown
            var tasksResult = await _apiClient.UpdateJobTasks.ExecuteAsync(updateInput);
            if (tasksResult.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error Saving Tasks", tasksResult.Errors[0].Message, "OK");
                return;
            }

            // 2. Submit the overall Approval to change the status
            Console.WriteLine($"[ScopeReview] Sending ApproveJobScope mutation...");
            var approveResult = await _apiClient.ApproveJobScope.ExecuteAsync(Job.Id, string.Empty); // We no longer need the big text scope

            if (approveResult.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Server Error", approveResult.Errors[0].Message, "OK");
                return;
            }

            await Shell.Current.DisplayAlert("Success", "Your task breakdown has been submitted to the Admin for final review.", "OK");
            
            // Redirect back to Project Details
            await Shell.Current.GoToAsync("//MyProjectsPage");
        }
        catch (Exception ex)
        {
            // Handle common transition errors gracefully
            if (ex.Message.Contains("WaitingForAdminReview") || ex.Message.Contains("status OPEN"))
            {
                await Shell.Current.GoToAsync("//MyProjectsPage");
            }
            else
            {
                await Shell.Current.DisplayAlert("System Error", ex.Message, "OK");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
