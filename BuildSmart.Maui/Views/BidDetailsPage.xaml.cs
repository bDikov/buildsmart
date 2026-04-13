using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class BidDetailsPage : ContentPage
{
    public BidDetailsPage(BidDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
