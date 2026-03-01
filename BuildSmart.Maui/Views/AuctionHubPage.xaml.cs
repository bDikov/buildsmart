using BuildSmart.Maui.ViewModels;
using BuildSmart.Maui.GraphQL;

namespace BuildSmart.Maui.Views;

public partial class AuctionHubPage : ContentPage
{
    private readonly AuctionHubViewModel _viewModel;

	public AuctionHubPage(AuctionHubViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

    private async void OnEditQuestionClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is IGetAuctionById_AuctionById_Questions question)
        {
            var currentId = _viewModel.CurrentTradesmanProfileId?.ToString();
            var ownerId = question.TradesmanProfileId.ToString();

            // Debug alert to see what's happening
            await Shell.Current.DisplayAlert("Debug ID Check", $"Current: {currentId}\nOwner: {ownerId}", "OK");

            if (string.Equals(currentId, ownerId, StringComparison.OrdinalIgnoreCase))
            {
                await _viewModel.EditQuestionCommand.ExecuteAsync(question);
            }
            else
            {
                await Shell.Current.DisplayAlert("Access Denied", "You can only edit your own questions.", "OK");
            }
        }
    }
}
