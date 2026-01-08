using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class CreateAccountPage : ContentPage
{
	public CreateAccountPage(CreateAccountPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
