using BuildSmart.Maui.ViewModels;
using BuildSmart.Maui.GraphQL;

namespace BuildSmart.Maui.Views;

public partial class MyProjectsPage : ContentPage
{
    private readonly MyProjectsViewModel _viewModel;

    public MyProjectsPage(MyProjectsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadProjectsCommand.ExecuteAsync(null);
    }

    private async void OnProjectTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is IGetMyProjects_MyProjects project)
        {
            await _viewModel.GoToDetailsCommand.ExecuteAsync(project);
        }
    }

    private async void OnDeleteProjectClicked(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem && swipeItem.CommandParameter is IGetMyProjects_MyProjects project)
        {
            await _viewModel.DeleteProjectCommand.ExecuteAsync(project);
        }
    }
}
