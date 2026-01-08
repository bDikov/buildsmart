using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class CategoryManagementPage : ContentPage
{
	public CategoryManagementPage(CategoryManagementViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
