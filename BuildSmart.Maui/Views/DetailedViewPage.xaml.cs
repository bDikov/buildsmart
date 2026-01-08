using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class DetailedViewPage : ContentPage
{
	private readonly DetailedViewPageViewModel _viewModel;

	public DetailedViewPage(DetailedViewPageViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.LoadUserAsync();
	}
}
