using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class UserProfilePage : ContentPage
{
	public UserProfilePage(UserProfileViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
