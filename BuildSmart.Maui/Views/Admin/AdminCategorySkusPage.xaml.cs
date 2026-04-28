using BuildSmart.SharedUI.ViewModels;
using BuildSmart.SharedUI.ViewModels.Admin;

namespace BuildSmart.Maui.Views.Admin;

public partial class AdminCategorySkusPage : ContentPage
{
    public AdminCategorySkusPage(AdminCategorySkusViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

