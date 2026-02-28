using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace BuildSmart.Maui.ViewModels.Admin;

public partial class CategorySelectionViewModel : ObservableObject
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}

public partial class UserEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly IBuildSmartApiClient _apiClient;

    public UserEditViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("User", out var userObj) && userObj is IGetUsers_Users user)
        {
            User = user;
            SelectedRole = user.Role;
            IsTradesmanFieldsVisible = user.Role == UserRoleTypes.Tradesman;
            await LoadCategoriesAsync();
        }
    }

    [ObservableProperty]
    private IGetUsers_Users? _user;

    [ObservableProperty]
    private UserRoleTypes _selectedRole;

    public List<UserRoleTypes> AllRoles => Enum.GetValues<UserRoleTypes>().ToList();

    [ObservableProperty]
    private ObservableCollection<CategorySelectionViewModel> _categories = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isTradesmanFieldsVisible;

    partial void OnSelectedRoleChanged(UserRoleTypes value)
    {
        IsTradesmanFieldsVisible = value == UserRoleTypes.Tradesman;
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _apiClient.GetServiceCategories.ExecuteAsync();
            
            Categories.Clear();
            if (result.Data?.ServiceCategories != null)
            {
                foreach (var cat in result.Data.ServiceCategories)
                {
                    var selection = new CategorySelectionViewModel
                    {
                        Id = cat.Id,
                        Name = cat.Name,
                        IsSelected = User?.TradesmanProfile?.Skills.Any(s => s.ServiceCategoryId == cat.Id) ?? false
                    };
                    Categories.Add(selection);
                }
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", "Failed to load categories: " + ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (User == null) return;

        try
        {
            IsBusy = true;
            
            var selectedCategoryIds = Categories
                .Where(c => c.IsSelected)
                .Select(c => c.Id)
                .ToList();

            var result = await _apiClient.UpdateUserRoleAndCategories.ExecuteAsync(
                User.Id, 
                SelectedRole, 
                SelectedRole == UserRoleTypes.Tradesman ? selectedCategoryIds : null);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            await Shell.Current.DisplayAlert("Success", "User updated successfully.", "OK");
            await Shell.Current.GoToAsync("..");
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
