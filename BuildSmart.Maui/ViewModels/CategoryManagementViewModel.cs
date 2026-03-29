using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.Maui.ViewModels;

public partial class CategoryManagementViewModel : ObservableObject
{
	private readonly IBuildSmartApiClient _apiClient;

	[ObservableProperty]
	private ObservableCollection<IGetAllServiceCategories_AllServiceCategories> _categories = new();

	[ObservableProperty]
	private bool _isBusy;

	public CategoryManagementViewModel(IBuildSmartApiClient apiClient)
	{
		_apiClient = apiClient;
		LoadCategoriesCommand.Execute(null);
	}

	[RelayCommand]
	private async Task LoadCategoriesAsync()
	{
		if (IsBusy) return;

		try
		{
			IsBusy = true;
			var result = await _apiClient.GetAllServiceCategories.ExecuteAsync();

			if (result.Errors.Count == 0 && result.Data?.AllServiceCategories is not null)
			{
				Categories.Clear();
				// Filter out Global categories from the main list
				foreach (var category in result.Data.AllServiceCategories.Where(c => !c.IsGlobal))
				{
					Categories.Add(category);
				}
			}
		}
		catch (System.Exception ex)
		{
			await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
		}
		finally
		{
			IsBusy = false;
		}
	}

    [RelayCommand]
    private async Task ManageGlobalQuestionsAsync()
    {
        try 
        {
            IsBusy = true;
            // Fetch all to find the global one
            var result = await _apiClient.GetAllServiceCategories.ExecuteAsync();
            var globalCat = result.Data?.AllServiceCategories?.FirstOrDefault(c => c.IsGlobal);

            if (globalCat != null)
            {
                // Edit existing
                var navParams = new Dictionary<string, object> { { "id", globalCat.Id.ToString() } };
                await Shell.Current.GoToAsync(nameof(Views.Admin.CategoryDetailPage), navParams);
            }
            else
            {
                // Create new Global Category
                var navParams = new Dictionary<string, object> { { "isGlobalMode", "true" } };
                await Shell.Current.GoToAsync(nameof(Views.Admin.CategoryDetailPage), navParams);
            }
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
	private async Task UpdateStatusAsync(Guid categoryId)
	{
		try 
		{
			var category = Categories.FirstOrDefault(c => c.Id == categoryId);
			if (category is null) return;

			var newStatus = category.Status == CategoryStatus.Draft
				? CategoryStatus.Active
				: CategoryStatus.Draft;

			var result = await _apiClient.UpdateCategoryStatus.ExecuteAsync(category.Id, newStatus);
			if (result.Errors.Count > 0) 
			{
				await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
				return;
			}

			// For now, just refresh the list
			await LoadCategoriesAsync();
		}
		catch (Exception ex)
		{
			await Shell.Current.DisplayAlert("Update Error", ex.Message, "OK");
		}
	}

	[RelayCommand]
	private async Task GoToDetailsAsync(Guid categoryId)
	{
		try
		{
			var navParams = new Dictionary<string, object> { { "id", categoryId.ToString() } };
			await Shell.Current.GoToAsync(nameof(Views.Admin.CategoryDetailPage), navParams);
		}
		catch (Exception ex)
		{
			await Shell.Current.DisplayAlert("Navigation Error", ex.Message, "OK");
		}
	}

	[RelayCommand]
	private async Task GoToNewCategoryDetailPageAsync()
	{
		await Shell.Current.GoToAsync(nameof(Views.Admin.CategoryDetailPage));
	}
}