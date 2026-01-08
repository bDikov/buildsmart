using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class TradesmanDetailsPage : ContentPage
{
    public TradesmanDetailsPage(TradesmanDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
