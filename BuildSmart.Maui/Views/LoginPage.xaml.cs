using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class LoginPage : ContentPage
{
	public LoginPage(LoginPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    private async void OnTestBlazorClicked(object sender, EventArgs e)
    {
        // Navigate to the BlazorHostPage we registered in MauiProgram.cs
        await Shell.Current.GoToAsync(nameof(BlazorHostPage));
    }
}
