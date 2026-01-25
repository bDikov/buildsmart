using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BuildSmart.Maui.ViewModels.Admin;

[QueryProperty(nameof(CategoryIdAsString), "id")]
public partial class CategoryDetailViewModel : ObservableObject
{
    public static List<string> QuestionTypes => new() { "text", "number", "boolean" };

    private readonly IBuildSmartApiClient _apiClient;

    public string CategoryIdAsString { set => OnSetCategoryId(value); }

    [ObservableProperty]
    private Guid? _categoryId;

    [ObservableProperty]
    private string _categoryName;

    [ObservableProperty]
    private string _categoryDescription;

    [ObservableProperty]
    private ObservableCollection<QuestionViewModel> _questions = new();

    public CategoryDetailViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
        _categoryName = string.Empty;
        _categoryDescription = string.Empty;
    }

    private void OnSetCategoryId(string idAsString)
    {
        if (Guid.TryParse(idAsString, out Guid result))
        {
            CategoryId = result;
            LoadCategoryDetailsAsync(result);
        }
        else
        {
            CategoryId = null;
        }
    }



    private async void LoadCategoryDetailsAsync(Guid id)

    {

        try

        {

            var result = await _apiClient.GetAllServiceCategories.ExecuteAsync();

            if (result.Errors.Count == 0 && result.Data?.AllServiceCategories is not null)

            {

                var category = result.Data.AllServiceCategories.FirstOrDefault(c => c.Id == id);

                if (category != null)

                {

                    CategoryName = category.Name;

                    CategoryDescription = category.Description ?? string.Empty;

                    

                    if (!string.IsNullOrWhiteSpace(category.TemplateStructure))

                    {

                        var template = JsonNode.Parse(category.TemplateStructure);

                        if (template?["questions"] is JsonArray questionNodes)

                        {

                            Questions.Clear();

                            foreach (var qNode in questionNodes)

                            {

                                if (qNode is JsonObject qObj)

                                {

                                    Questions.Add(new QuestionViewModel

                                    {                                        Id = qObj["id"]?.GetValue<string>() ?? string.Empty,

                                        Text = qObj["text"]?.GetValue<string>() ?? string.Empty,

                                        Type = qObj["type"]?.GetValue<string>() ?? "text"

                                    });

                                }

                            }

                        }

                    }

                }

            }

        }

        catch (Exception ex)

        {

            await Shell.Current.DisplayAlert("Load Error", ex.ToString(), "OK");

        }

    }



    [RelayCommand]

    private void AddNewQuestion()

    {

        Questions.Add(new QuestionViewModel());

    }



    [RelayCommand]

    private void RemoveQuestion(QuestionViewModel question)

    {

        if (question != null)

        {

            Questions.Remove(question);

        }

    }



    [RelayCommand]

    private async Task SaveCategoryAsync()

    {

        try

        {

            var questionNodes = new JsonArray(

                Questions.Select(q => new JsonObject

                {

                    ["id"] = q.Id,

                    ["text"] = q.Text,

                    ["type"] = q.Type

                }).ToArray());



            var template = new JsonObject

            {

                ["questions"] = questionNodes

            };

            

            var templateStructureJson = template.ToJsonString();

            

            // The CategoryId property is already a Guid?, so we can pass it directly.

            var result = await _apiClient.SaveCategory.ExecuteAsync(CategoryId, CategoryName, CategoryDescription, templateStructureJson);



            if (result.Errors.Count > 0)

            {

                 var errorMsg = string.Join("\n", result.Errors.Select(e => e.Message));

                 await Shell.Current.DisplayAlert("Save Failed", errorMsg, "OK");

                 return;

            }



            await Shell.Current.DisplayAlert("Saved", "Category has been saved.", "OK");

            await Shell.Current.GoToAsync("..");

        }

        catch (Exception ex)

        {

            await Shell.Current.DisplayAlert("Error Saving", ex.ToString(), "OK");

        }

    }

}
