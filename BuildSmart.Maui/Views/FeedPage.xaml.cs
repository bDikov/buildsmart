using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class FeedPage : ContentPage
{
    private readonly FeedPageViewModel _viewModel;

	public FeedPage(FeedPageViewModel viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadTradesmenCommand.ExecuteAsync(null);
    }
}
