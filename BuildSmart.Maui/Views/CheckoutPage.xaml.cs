using BuildSmart.SharedUI.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class CheckoutPage : ContentPage
{
    public CheckoutPage(CheckoutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

