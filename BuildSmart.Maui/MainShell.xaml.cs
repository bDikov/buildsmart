using BuildSmart.Maui.Services;

namespace BuildSmart.Maui;

public partial class MainShell : Shell
{
	private readonly IAuthService _authService;
	private readonly IServiceProvider _serviceProvider;

	public MainShell(IAuthService authService, IServiceProvider serviceProvider)
	{
		_authService = authService;
		_serviceProvider = serviceProvider;
		InitializeComponent();
	}

	private async void OnProfileSettingsClicked(object sender, EventArgs e)
	{
		await DisplayAlert("Profile Settings", "Profile settings coming soon!", "OK");
	}

	private async void OnLogoutClicked(object sender, EventArgs e)
	{
		bool answer = await DisplayAlert("Logout", "Are you sure you want to log out?", "Yes", "No");
		if (!answer) return;

		await _authService.ClearTokenAsync();
		Application.Current.MainPage = _serviceProvider.GetRequiredService<AppShell>();
	}
}
