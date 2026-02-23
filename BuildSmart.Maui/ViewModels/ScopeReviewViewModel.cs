using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;

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
            // Prefer UserEditedScope if they've already started editing, otherwise use GeneratedScope
            EditableScope = !string.IsNullOrEmpty(job.UserEditedScope) 
                ? job.UserEditedScope 
                : (job.GeneratedScope ?? string.Empty);
            
            Console.WriteLine($"[ScopeReview] SUCCESS: Job loaded with ID: {Job.Id}. Scope Length: {EditableScope.Length}");
        }
        else
        {
            Console.WriteLine("[ScopeReview] ERROR: Received invalid Job object or ID.");
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
        if (Job == null)
        {
            await Shell.Current.DisplayAlert("Error", "Job data is missing. Please go back and try again.", "OK");
            return;
        }

        if (Job.Id == Guid.Empty)
        {
            await Shell.Current.DisplayAlert("Error", "Job ID is empty. This is a synchronization error. Please refresh your projects.", "OK");
            return;
        }

        if (IsBusy) return;

        try
        {
            IsBusy = true;
            Console.WriteLine($"[ScopeReview] Sending Approve mutation for Job: {Job.Id}...");
            
            var result = await _apiClient.ApproveJobScope.ExecuteAsync(Job.Id, EditableScope ?? string.Empty);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Server Error", result.Errors[0].Message, "OK");
                return;
            }

            await Shell.Current.DisplayAlert("Success", "Scope approved and sent to Admin for final review.", "OK");
            
            // Redirect back to Project Details
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            // Handle common transition errors gracefully
            if (ex.Message.Contains("WaitingForAdminReview") || ex.Message.Contains("status OPEN"))
            {
                await Shell.Current.GoToAsync("..");
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
