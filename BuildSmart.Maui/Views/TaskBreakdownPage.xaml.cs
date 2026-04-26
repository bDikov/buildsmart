using BuildSmart.SharedUI.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class TaskBreakdownPage : ContentPage
{
    public TaskBreakdownPage(TaskBreakdownViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

