using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class LoginPage : ContentPage
{
	public LoginPage(LoginPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
