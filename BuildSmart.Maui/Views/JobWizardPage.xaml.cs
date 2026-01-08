using BuildSmart.Maui.ViewModels;

namespace BuildSmart.Maui.Views;

public partial class JobWizardPage : ContentPage
{
	public JobWizardPage(JobWizardViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
