using BuildSmart.SharedUI.ViewModels;
using Microsoft.AspNetCore.Components.WebView.Maui;

namespace BuildSmart.Maui.Views;

public partial class AuctionHubPage : ContentPage
{
	private readonly AuctionHubViewModel _viewModel;

	public AuctionHubPage(AuctionHubViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;

		var blazorWebView = new BlazorWebView
		{
			HostPage = "wwwroot/index.html"
		};

		blazorWebView.RootComponents.Add(new RootComponent
		{
			Selector = "#app",
			ComponentType = typeof(BuildSmart.SharedUI.Components.Pages.AuctionHub),
			Parameters = new Dictionary<string, object?>
			{
				{ "ViewModel", _viewModel }
			}
		});

		Content = blazorWebView;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.InitializeAsync();
	}

	protected override async void OnDisappearing()
	{
		base.OnDisappearing();
		await _viewModel.CleanupAsync();
	}
}