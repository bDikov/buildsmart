using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class BidDetailsPage : ContentPage
{
    private readonly BidDetailsViewModel _viewModel;

    public BidDetailsPage(BidDetailsViewModel viewModel)
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
