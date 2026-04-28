using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.SharedUI.ViewModels.Admin;

public partial class UserManagementViewModel : ObservableObject
{
    private readonly IBuildSmartApiClient _apiClient;

    public UserManagementViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [ObservableProperty]
    private ObservableCollection<IGetUsers_Users> _users = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isEmpty;

    [RelayCommand]
    public async Task LoadUsersAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var result = await _apiClient.GetUsers.ExecuteAsync();

            if (result.Errors.Count > 0)
            {
                await AppServiceLocator.Alerts.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            Users.Clear();
            if (result.Data?.Users != null)
            {
                foreach (var user in result.Data.Users)
                {
                    Users.Add(user);
                }
            }
            
            IsEmpty = !Users.Any();
        }
        catch (Exception ex)
        {
            await AppServiceLocator.Alerts.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EditUser(IGetUsers_Users user)
    {
        if (user == null) return;
        
        var navigationParameter = new Dictionary<string, object>
        {
            { "User", user }
        };
        await AppServiceLocator.Navigation.NavigateToAsync("UserEditPage", navigationParameter);
    }
}






