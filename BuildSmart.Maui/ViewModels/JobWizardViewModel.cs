using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BuildSmart.Maui.ViewModels;

public partial class JobWizardViewModel : ObservableObject
{
    private readonly IBuildSmartApiClient _apiClient;

    [ObservableProperty]
    private ObservableCollection<SelectableCategoryViewModel> _selectableCategories = new();

    private List<SelectableCategoryViewModel> _allCategories = new();

    [ObservableProperty]
    private string _projectTitle = string.Empty;
    
    [ObservableProperty]
    private string _projectDescription = string.Empty;

    [ObservableProperty]
    private string _projectLocation = string.Empty;

    [ObservableProperty]
    private bool _titleHasError;

    [ObservableProperty]
    private bool _descriptionHasError;

    [ObservableProperty]
    private bool _locationHasError;

    [ObservableProperty]
    private bool _categorySelectionHasError;

    // This will be used in a later step for dynamic forms
    public Dictionary<string, object> WizardAnswers { get; private set; } = new();

    [ObservableProperty]
    private ObservableCollection<WizardQuestionViewModel> _questions = new();

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
                _allCategories.Clear();

                foreach (var cat in result.Data.ServiceCategories)
                {
                    var viewModel = new SelectableCategoryViewModel(cat);
                    _allCategories.Add(viewModel);

                    if (!cat.IsGlobal)
                    {
                        SelectableCategories.Add(viewModel);
                    }
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
    public async Task GoToNextStep()
    {
        if (CurrentStep == 0)
        {
            TitleHasError = string.IsNullOrWhiteSpace(ProjectTitle);
            DescriptionHasError = string.IsNullOrWhiteSpace(ProjectDescription);
            LocationHasError = string.IsNullOrWhiteSpace(ProjectLocation);

            if (TitleHasError || DescriptionHasError || LocationHasError)
            {
                await Shell.Current.DisplayAlert("Required", "Please enter a project title, description, and location.", "OK");
                return;
            }
        }

        if (CurrentStep == 1)
        {
             if (!_selectableCategories.Any(c => c.IsSelected))
             {
                 CategorySelectionHasError = true;
                 await Shell.Current.DisplayAlert("Required", "Please select at least one category.", "OK");
                 return;
             }
             CategorySelectionHasError = false;
             GenerateQuestions();
        }

        if (CurrentStep == 2)
        {
            // Reset errors first
            foreach (var q in Questions) q.HasError = false;

            var missingQuestions = Questions.Where(q => q.IsRequired && string.IsNullOrWhiteSpace(q.Answer)).ToList();
            
            if (missingQuestions.Any())
            {
                foreach (var q in missingQuestions)
                {
                    q.HasError = true;
                }
                
                await Shell.Current.DisplayAlert("Required", "Please answer all required questions marked with (*).", "OK");
                return;
            }
        }

        // In a real app, you'd validate here, e.g., ensure at least one category is selected
        CurrentStep++;
    }

    private void GenerateQuestions()
    {
        Questions.Clear();
        
        // 1. Get Global Categories (Available in the full list loaded in LoadCategoriesAsync)
        // We need to access the full list. Since SelectableCategories is just a view model wrapper, 
        // we might need to store the raw data or check the properties.
        // Assuming SelectableCategories was populated from All Service Categories.
        
        // However, LoadCategoriesAsync calls GetServiceCategories.ExecuteAsync().
        // We updated that query to include 'isGlobal'.
        
        var globalCategories = _allCategories.Where(c => c.Category.IsGlobal).ToList();
        var selectedCategories = SelectableCategories.Where(c => c.IsSelected).ToList();
        
        // Combine: Global first, then Selected
        // Avoid duplicates if a global category is also selected (though unlikely in UI if we filter)
        var allApplicableCategories = globalCategories.Union(selectedCategories).ToList();

        foreach (var cat in allApplicableCategories)
        {
            if (!string.IsNullOrWhiteSpace(cat.Category.TemplateStructure))
            {
                try
                {
                    var template = JsonNode.Parse(cat.Category.TemplateStructure);
                    if (template?["questions"] is JsonArray qArray)
                    {
                        foreach (var qNode in qArray)
                        {
                            if (qNode is JsonObject qObj)
                            {
                                Questions.Add(new WizardQuestionViewModel
                                {
                                    Id = qObj["id"]?.GetValue<string>() ?? "",
                                    Text = qObj["text"]?.GetValue<string>() ?? "",
                                    Type = qObj["type"]?.GetValue<string>() ?? "text",
                                    IsRequired = qObj["required"]?.GetValue<bool>() ?? false,
                                    CategoryName = cat.Category.Name
                                });
                            }
                        }
                    }
                }
                catch { /* Ignore parse errors */ }
            }
        }
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

        // Validate Required Questions
        var missingAnswers = Questions.Where(q => q.IsRequired && string.IsNullOrWhiteSpace(q.Answer)).ToList();
        if (missingAnswers.Any())
        {
            await Shell.Current.DisplayAlert("Required", "Please answer all required questions marked with (*).", "OK");
            return;
        }

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
                 var errorMsg = string.Join("\n", projectResult.Errors.Select(e => e.Message));
                 await Shell.Current.DisplayAlert("Error", $"Failed to create project: {errorMsg}", "OK");
                 return;
            }
            var projectId = projectResult.Data.CreateProject.Id;

            // Prepare answers
            WizardAnswers.Clear();
            var dynamicAnswers = Questions.Select(q => new
            {
                id = q.Id,
                question = q.Text,
                answer = q.Answer
            }).ToList();
            
            WizardAnswers["dynamicQuestions"] = dynamicAnswers;

            // Loop and create a job for each selected category
            foreach (var selectedCategory in selected)
            {
                var answersJson = JsonSerializer.Serialize(WizardAnswers);
                var jobResult = await _apiClient.AddJobToProject.ExecuteAsync(
                    projectId,
                    selectedCategory.Category.Id,
                    selectedCategory.Category.Name, // Use category name for the job title
                    answersJson,
                    ProjectLocation,
                    null, "USD", new List<string>()
                );

                if (jobResult.Errors.Count > 0)
                {
                    var msg = string.Join(", ", jobResult.Errors.Select(e => e.Message));
                    await Shell.Current.DisplayAlert("Job Creation Failed", $"Failed to add job for {selectedCategory.Category.Name}: {msg}", "OK");
                }
            }

            await Shell.Current.DisplayAlert("Success", "Project posted with all selected jobs!", "OK");
            await Shell.Current.GoToAsync("//MyProjectsPage");
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
