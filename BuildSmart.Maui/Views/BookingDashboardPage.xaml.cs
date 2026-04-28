using BuildSmart.SharedUI.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class BookingDashboardPage : ContentPage
{
    public BookingDashboardPage(BookingDashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

