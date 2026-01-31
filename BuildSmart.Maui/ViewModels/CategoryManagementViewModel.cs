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
                await Shell.Current.GoToAsync($"{nameof(Views.Admin.CategoryDetailPage)}?id={globalCat.Id}");
            }
            else
            {
                // Create new Global Category (handled by passing a special flag or just creating it here?)
                // Better UX: Pass a flag "isGlobalMode=true" to the detail page
                await Shell.Current.GoToAsync($"{nameof(Views.Admin.CategoryDetailPage)}?isGlobalMode=true");
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
	private async Task UpdateStatusAsync(IGetAllServiceCategories_AllServiceCategories category)
	{
		if (category is null) return;

		var newStatus = category.Status == CategoryStatus.Draft
			? CategoryStatus.Active
			: CategoryStatus.Draft;

		await _apiClient.UpdateCategoryStatus.ExecuteAsync(category.Id, newStatus);

		// For now, just refresh the list
		await LoadCategoriesAsync();
	}

	[RelayCommand]
	private async Task GoToDetailsAsync(Guid categoryId)
	{
		await Shell.Current.GoToAsync($"{nameof(Views.Admin.CategoryDetailPage)}?id={categoryId.ToString()}");
	}

	[RelayCommand]
	private async Task GoToNewCategoryDetailPageAsync()
	{
		await Shell.Current.GoToAsync(nameof(Views.Admin.CategoryDetailPage));
	}
}