using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class BookingPage : ContentPage
{
	public BookingPage(BookingPageViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
	}
}
