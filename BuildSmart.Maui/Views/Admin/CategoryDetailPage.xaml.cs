using BuildSmart.Maui.ViewModels.Admin;

namespace BuildSmart.Maui.Views.Admin;

public partial class CategoryDetailPage : ContentPage
{
	public CategoryDetailPage(CategoryDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
