using BuildSmart.Maui.ViewModels.Admin;

namespace BuildSmart.Maui.Views.Admin;

public partial class UserManagementPage : ContentPage
{
	public UserManagementPage(UserManagementViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var vm = (UserManagementViewModel)BindingContext;
        await vm.LoadUsersCommand.ExecuteAsync(null);
    }
}
