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
        await _viewModel.LoadFeedCommand.ExecuteAsync(null);
    }

    private async void OnViewAndBidClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter != null)
        {
            await _viewModel.NavigateToDetailsCommand.ExecuteAsync(button.CommandParameter);
        }
    }
}
