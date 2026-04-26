using BuildSmart.SharedUI.ViewModels;
using Microsoft.AspNetCore.Components.WebView.Maui;

namespace BuildSmart.Maui.Views;

public partial class JobWizardPage : ContentPage
{
    private readonly JobWizardViewModel _viewModel;

	public JobWizardPage(JobWizardViewModel viewModel)
	{
		InitializeComponent();
        _viewModel = viewModel;
		BindingContext = _viewModel;

        var blazorWebView = new BlazorWebView
        {
            HostPage = "wwwroot/index.html"
        };
        
        blazorWebView.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(BuildSmart.SharedUI.Components.Pages.JobWizard)
        });

        Content = blazorWebView;
	}
}

