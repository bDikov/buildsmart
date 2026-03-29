using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class PlaceBidPage : ContentPage
{
    public PlaceBidPage(PlaceBidViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
