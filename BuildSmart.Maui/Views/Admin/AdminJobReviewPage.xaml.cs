using BuildSmart.Maui.ViewModels.Admin;

namespace BuildSmart.Maui.Views.Admin;

public partial class AdminJobReviewPage : ContentPage
{
	public AdminJobReviewPage(AdminJobReviewViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is AdminJobReviewViewModel vm)
        {
            await vm.LoadJobsAsync();
        }
    }
}
