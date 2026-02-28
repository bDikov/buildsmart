using BuildSmart.Maui.ViewModels.Admin;

namespace BuildSmart.Maui.Views.Admin;

public partial class UserEditPage : ContentPage
{
	public UserEditPage(UserEditViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
