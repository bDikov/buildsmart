using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class MyProjectsPage : ContentPage
{
	public MyProjectsPage(MyProjectsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is MyProjectsViewModel viewModel)
        {
            await viewModel.LoadProjectsCommand.ExecuteAsync(null);
        }
    }
}
