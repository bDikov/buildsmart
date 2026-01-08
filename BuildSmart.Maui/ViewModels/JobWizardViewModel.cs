using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace BuildSmart.Maui.ViewModels;

public partial class JobWizardViewModel : ObservableObject
{
    private readonly IBuildSmartApiClient _apiClient;

    [ObservableProperty]
    private ObservableCollection<SelectableCategoryViewModel> _selectableCategories = new();

    [ObservableProperty]
    private string _projectTitle = string.Empty;
    
    [ObservableProperty]
    private string _projectDescription = string.Empty;

    // This will be used in a later step for dynamic forms
    public Dictionary<string, object> WizardAnswers { get; private set; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private int _currentStep = 0;

    public JobWizardViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
        LoadCategoriesAsync();
    }

    [RelayCommand]
    public async Task LoadCategoriesAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _apiClient.GetServiceCategories.ExecuteAsync();

            if (result.Errors.Count > 0)
            {
                var errorMessages = string.Join(", ", result.Errors.Select(e => e.Message));
                await Shell.Current.DisplayAlert("GraphQL Error", errorMessages, "OK");
            }
            else if (result.Data?.ServiceCategories != null)
            {
                SelectableCategories.Clear();
                foreach (var cat in result.Data.ServiceCategories)
                {
                    SelectableCategories.Add(new SelectableCategoryViewModel(cat));
                }
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to load categories: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void GoToNextStep()
    {
        // In a real app, you'd validate here, e.g., ensure at least one category is selected
        CurrentStep++;
    }

    [RelayCommand]
    public void GoToPreviousStep()
    {
        if (CurrentStep > 0) CurrentStep--;
    }

    [RelayCommand]
    public async Task SubmitProjectAsync()
    {
        if (IsBusy) return;

        var selected = SelectableCategories.Where(c => c.IsSelected).ToList();
        if (!selected.Any())
        {
            await Shell.Current.DisplayAlert("Error", "Please select at least one category.", "OK");
            return;
        }

        try
        {
            IsBusy = true;

            var userResult = await _apiClient.GetCurrentUser.ExecuteAsync();
            if (userResult.Errors.Count > 0 || userResult.Data?.CurrentUser == null)
            {
                await Shell.Current.DisplayAlert("Error", "Please login first.", "OK");
                return;
            }
            var userId = userResult.Data.CurrentUser.Id;

            var projectResult = await _apiClient.CreateProject.ExecuteAsync(userId, ProjectTitle, ProjectDescription);
            if (projectResult.Errors.Count > 0)
            {
                 await Shell.Current.DisplayAlert("Error", "Failed to create project.", "OK");
                 return;
            }
            var projectId = projectResult.Data.CreateProject.Id;

            // Loop and create a job for each selected category
            foreach (var selectedCategory in selected)
            {
                var answersJson = JsonSerializer.Serialize(WizardAnswers);
                await _apiClient.AddJobToProject.ExecuteAsync(
                    projectId,
                    selectedCategory.Category.Id,
                    selectedCategory.Category.Name, // Use category name for the job title
                    answersJson,
                    null, "USD", new List<string>()
                );
            }

            await Shell.Current.DisplayAlert("Success", "Project posted with all selected jobs!", "OK");
            await Shell.Current.GoToAsync("//FeedPage");
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
