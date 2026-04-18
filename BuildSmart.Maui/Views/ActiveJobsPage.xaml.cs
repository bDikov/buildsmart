using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class ActiveJobsPage : ContentPage
{
    private readonly ActiveJobsViewModel _viewModel;

    public ActiveJobsPage(ActiveJobsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
