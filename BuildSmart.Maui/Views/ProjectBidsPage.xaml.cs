using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class ProjectBidsPage : ContentPage
{
    public ProjectBidsPage(ProjectBidsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
