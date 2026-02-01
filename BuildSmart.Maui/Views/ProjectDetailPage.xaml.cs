using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class ProjectDetailPage : ContentPage
{
	public ProjectDetailPage(ProjectDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
