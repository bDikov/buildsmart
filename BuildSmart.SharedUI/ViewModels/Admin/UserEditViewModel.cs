using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.MauiMocks;
using BuildSmart.SharedUI.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace BuildSmart.SharedUI.ViewModels.Admin;

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
            await InitializeUserAsync(user);
        }
    }

    public async Task InitializeUserAsync(IGetUsers_Users user)
    {
        User = user;
        SelectedRole = user.Role;
        IsTradesmanFieldsVisible = user.Role == UserRoleTypes.Tradesman;
        await LoadCategoriesAsync();
        UpdateCategoriesSelection();
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
        UpdateCategoriesSelection();
    }

    public async Task LoadCategoriesAsync()
    {
        try
        {
            IsBusy = true;
            Categories.Clear();

            // Try loading all categories (including admin draft/active)
            var allResult = await _apiClient.GetAllServiceCategories.ExecuteAsync();
            if (allResult.Data?.AllServiceCategories != null && allResult.Data.AllServiceCategories.Count > 0)
            {
                foreach (var cat in allResult.Data.AllServiceCategories)
                {
                    if (cat.Type != CategoryType.CategorySpecific || cat.IsGlobal)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(cat.TemplateStructure) && (cat.TemplateStructure.Contains("\"isProjectDetails\": true") || cat.TemplateStructure.Contains("\"isProjectDetails\":true")))
                    {
                        continue;
                    }

                    var selection = new CategorySelectionViewModel
                    {
                        Id = cat.Id,
                        Name = cat.Name,
                        IsSelected = false
                    };
                    Categories.Add(selection);
                }
            }

            // Fallback if GetAllServiceCategories is empty or null
            if (Categories.Count == 0)
            {
                var result = await _apiClient.GetServiceCategories.ExecuteAsync();
                if (result.Data?.ServiceCategories != null)
                {
                    foreach (var cat in result.Data.ServiceCategories)
                    {
                        if (cat.Type != CategoryType.CategorySpecific || cat.IsGlobal)
                        {
                            continue;
                        }

                        if (!string.IsNullOrEmpty(cat.TemplateStructure) && (cat.TemplateStructure.Contains("\"isProjectDetails\": true") || cat.TemplateStructure.Contains("\"isProjectDetails\":true")))
                        {
                            continue;
                        }

                        var selection = new CategorySelectionViewModel
                        {
                            Id = cat.Id,
                            Name = cat.Name,
                            IsSelected = false
                        };
                        Categories.Add(selection);
                    }
                }
            }

            UpdateCategoriesSelection();
        }
        catch (Exception ex)
        {
            await AppServiceLocator.Alerts.DisplayAlert("Error", "Failed to load categories: " + ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void UpdateCategoriesSelection()
    {
        var tradesmanSkillGuids = new HashSet<Guid>();

        if (User?.TradesmanProfile?.Skills != null)
        {
            foreach (var skill in User.TradesmanProfile.Skills)
            {
                if (string.IsNullOrWhiteSpace(skill.ServiceCategoryId))
                    continue;

                if (Guid.TryParse(skill.ServiceCategoryId, out var skillGuid))
                {
                    tradesmanSkillGuids.Add(skillGuid);
                }
                else
                {
                    var cleanSkillId = skill.ServiceCategoryId.Replace("-", "");
                    if (Guid.TryParseExact(cleanSkillId, "N", out var parsedN))
                    {
                        tradesmanSkillGuids.Add(parsedN);
                    }
                }
            }
        }

        foreach (var cat in Categories)
        {
            cat.IsSelected = tradesmanSkillGuids.Contains(cat.Id);
        }
    }

    [RelayCommand]
    public async Task<bool> SaveAsync()
    {
        if (User == null) return false;

        try
        {
            IsBusy = true;
            
            var selectedCategoryIds = Categories
                .Where(c => c.IsSelected)
                .Select(c => c.Id)
                .ToList();

            var result = await _apiClient.UpdateUserRoleAndCategories.ExecuteAsync(
                Guid.Parse(User.Id), 
                SelectedRole, 
                SelectedRole == UserRoleTypes.Tradesman ? selectedCategoryIds : null);

            if (result.Errors != null && result.Errors.Count > 0)
            {
                await AppServiceLocator.Alerts.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return false;
            }

            await AppServiceLocator.Alerts.DisplayAlert("Success", "User updated successfully.", "OK");
            return true;
        }
        catch (Exception ex)
        {
            await AppServiceLocator.Alerts.DisplayAlert("Error", ex.Message, "OK");
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}





