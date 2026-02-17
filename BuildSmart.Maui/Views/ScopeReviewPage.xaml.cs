using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class ScopeReviewPage : ContentPage
{
	public ScopeReviewPage(ScopeReviewViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
