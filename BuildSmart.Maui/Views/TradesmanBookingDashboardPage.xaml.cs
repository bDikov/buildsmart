using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class TradesmanBookingDashboardPage : ContentPage
{
    public TradesmanBookingDashboardPage(TradesmanBookingDashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}