using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class AuctionHubPage : ContentPage
{
	public AuctionHubPage(AuctionHubViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
