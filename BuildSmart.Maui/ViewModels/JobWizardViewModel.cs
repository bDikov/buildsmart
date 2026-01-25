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

    [ObservableProperty]
    private string _projectTitle = string.Empty;
    
    [ObservableProperty]
    private string _projectDescription = string.Empty;

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
    public async Task GoToNextStep()
    {
        if (CurrentStep == 0)
        {
            if (string.IsNullOrWhiteSpace(ProjectTitle))
            {
                await Shell.Current.DisplayAlert("Required", "Please enter a project title.", "OK");
                return;
            }
            if (string.IsNullOrWhiteSpace(ProjectDescription))
            {
                await Shell.Current.DisplayAlert("Required", "Please enter a project description.", "OK");
                return;
            }
        }

        if (CurrentStep == 1)
        {
             if (!_selectableCategories.Any(c => c.IsSelected))
             {
                 await Shell.Current.DisplayAlert("Required", "Please select at least one category.", "OK");
                 return;
             }
             GenerateQuestions();
        }

        // In a real app, you'd validate here, e.g., ensure at least one category is selected
        CurrentStep++;
    }

    private void GenerateQuestions()
    {
        Questions.Clear();
        var selected = SelectableCategories.Where(c => c.IsSelected).ToList();
        foreach (var cat in selected)
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
