using BuildSmart.Maui.ViewModels;
using BuildSmart.Maui.GraphQL;

namespace BuildSmart.Maui.Views;

public partial class PassedAuctionsPage : ContentPage
{
    private readonly PassedAuctionsViewModel _viewModel;

    public PassedAuctionsPage(PassedAuctionsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadPassedAuctionsAsync();
    }

    private async void OnRestoreClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IGetPassedAuctions_PassedAuctions auction)
        {
            await _viewModel.RestoreAuctionCommand.ExecuteAsync(auction);
        }
    }
}
