using BuildSmart.Maui.Views;

namespace BuildSmart.Maui;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(DetailedViewPage), typeof(DetailedViewPage));
		Routing.RegisterRoute(nameof(CreateAccountPage), typeof(CreateAccountPage));
	}
}
