using BuildSmart.SharedUI.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class GeneratedOfferPage : ContentPage
{
    public GeneratedOfferPage(GeneratedOfferViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
