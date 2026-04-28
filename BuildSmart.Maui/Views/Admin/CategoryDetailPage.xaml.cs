using BuildSmart.SharedUI.ViewModels;
using BuildSmart.SharedUI.ViewModels.Admin;

namespace BuildSmart.Maui.Views.Admin;

public partial class CategoryDetailPage : ContentPage
{
	public CategoryDetailPage(CategoryDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}

