using BuildSmart.Maui.ViewModels;
using Microsoft.AspNetCore.Components.WebView.Maui;

namespace BuildSmart.Maui.Views;

public partial class FeedPage : ContentPage
{
    private readonly FeedPageViewModel _viewModel;

	public FeedPage(FeedPageViewModel viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        var blazorWebView = new BlazorWebView
        {
            HostPage = "wwwroot/index.html"
        };
        
        blazorWebView.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(Components.Pages.Feed)
        });

        Content = blazorWebView;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadFeedCommand.ExecuteAsync(null);
    }
}
