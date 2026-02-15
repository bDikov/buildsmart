using BuildSmart.Maui.Services;
using BuildSmart.Maui.Views;

namespace BuildSmart.Maui;

public partial class AdminShell : Shell
{
	private readonly IAuthService _authService;
	private readonly IServiceProvider _serviceProvider;
    private readonly SignalRService _signalRService;

	public AdminShell(IAuthService authService, IServiceProvider serviceProvider, SignalRService signalRService)
	{
		_authService = authService;
		_serviceProvider = serviceProvider;
        _signalRService = signalRService;
		InitializeComponent();

        // Connect to SignalR
        MainThread.BeginInvokeOnMainThread(async () => await _signalRService.ConnectAsync());
	}

	private async void OnProfileSettingsClicked(object sender, EventArgs e)
	{
		Shell.Current.FlyoutIsPresented = false;
		await Shell.Current.GoToAsync(nameof(UserProfilePage));
	}

	private async void OnLogoutClicked(object sender, EventArgs e)
	{
		bool answer = await DisplayAlert("Logout", "Are you sure you want to log out?", "Yes", "No");
		if (!answer) return;

		await _authService.ClearTokenAsync();
		Application.Current.MainPage = _serviceProvider.GetRequiredService<AppShell>();
	}
}