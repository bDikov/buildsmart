using Microsoft.Extensions.Logging;
using BuildSmart.Maui.Views;
using BuildSmart.SharedUI.ViewModels;
using BuildSmart.Maui.Services;
using BuildSmart.SharedUI.Services;
using BuildSmart.Maui.Views.Admin;
using BuildSmart.SharedUI.ViewModels.Admin;
using BuildSmart.SharedUI.Handlers;
using BuildSmart.SharedUI;

namespace BuildSmart.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		// Configure SharedUI API Config based on MAUI platform
		if (Microsoft.Maui.Devices.DeviceInfo.Current.Platform == Microsoft.Maui.Devices.DevicePlatform.Android)
		{
			BuildSmart.SharedUI.ApiConfig.BaseUrlOverride = "https://10.0.2.2:7212";
		}
		else
		{
			BuildSmart.SharedUI.ApiConfig.BaseUrlOverride = "https://localhost:7212";
		}

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddMauiBlazorWebView();
#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        builder.Services.AddLocalization();

		// Configure StrawberryShake GraphQL Client

		builder.Services.AddSingleton<BuildSmart.SharedUI.MauiMocks.IMediaPicker, BuildSmart.Maui.Services.AppMediaPicker>();
		builder.Services.AddSingleton<BuildSmart.SharedUI.MauiMocks.IFilePicker, BuildSmart.Maui.Services.AppFilePicker>(); // Good practice to add commonly used essentials

		// Services
		builder.Services.AddSingleton<IAuthService, AuthService>();
		builder.Services.AddSingleton<SignalRService>(); // Added SignalRService
		builder.Services.AddSingleton<IFileService, FileService>();
		builder.Services.AddSingleton<IBlazorNavigationRegistry, BlazorNavigationRegistry>();
		builder.Services.AddSingleton<INavigationBridge, NavigationBridge>();
		builder.Services.AddSingleton<IAlertService, AlertService>();
		builder.Services.AddSingleton<IAppMainThread, AppMainThread>();
		builder.Services.AddHttpClient();
		builder.Services.AddTransient<AuthHeaderHandler>();
		builder.Services.AddTransient<LoggingHandler>();

		// Blazor Authentication & Authorization
		builder.Services.AddAuthorizationCore();
		builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, MauiAuthenticationStateProvider>();

		// Register Strawberry Shake with fluent configuration
		builder.Services.AddBuildSmartApiClient()
			.ConfigureHttpClient(client =>
			{
				client.BaseAddress = new Uri(ApiConfig.GetGraphQLUrl());
				// Force HTTP/1.1 for local development compatibility
				client.DefaultRequestVersion = System.Net.HttpVersion.Version11;
				client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
			}, static builder =>
			{
				builder.ConfigurePrimaryHttpMessageHandler(static () =>
				{
					return new HttpClientHandler
					{
						ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
					};
				})
				.AddHttpMessageHandler<LoggingHandler>()
				.AddHttpMessageHandler<AuthHeaderHandler>();
			});

		builder.Services.AddSingleton<LoginPage>(); builder.Services.AddSingleton<LoginPageViewModel>();
		builder.Services.AddTransient<DetailedViewPageViewModel>();
		builder.Services.AddTransient<CreateAccountPage>();
		builder.Services.AddTransient<CreateAccountPageViewModel>();

		builder.Services.AddTransient<FeedPage>();
		builder.Services.AddTransient<FeedPageViewModel>();

		builder.Services.AddTransient<TradesmanDetailsViewModel>();

		builder.Services.AddTransient<BookingPage>();
		builder.Services.AddTransient<BookingPageViewModel>();

		builder.Services.AddTransient<JobWizardViewModel>();

		builder.Services.AddTransient<UserProfileViewModel>();

		builder.Services.AddTransient<MyProjectsViewModel>();

		builder.Services.AddTransient<ProjectDetailViewModel>();

		builder.Services.AddTransient<ScopeReviewPage>();
		builder.Services.AddTransient<ScopeReviewViewModel>();

		builder.Services.AddTransient<GeneratedOfferViewModel>();

		builder.Services.AddTransient<NotificationsViewModel>();

		builder.Services.AddTransient<AuctionHubPage>();
		builder.Services.AddTransient<AuctionHubViewModel>();

		builder.Services.AddTransient<TaskBreakdownPage>();
		builder.Services.AddTransient<TaskBreakdownViewModel>();

		builder.Services.AddTransient<BidDetailsPage>();
		builder.Services.AddTransient<BidDetailsViewModel>();

		builder.Services.AddTransient<CheckoutPage>();
		builder.Services.AddTransient<CheckoutViewModel>();

		builder.Services.AddTransient<BookingDashboardPage>();
		builder.Services.AddTransient<BookingDashboardViewModel>();

		builder.Services.AddTransient<ActiveJobsViewModel>();

		builder.Services.AddTransient<TradesmanBookingDashboardPage>();
		builder.Services.AddTransient<TradesmanBookingDashboardViewModel>();

		builder.Services.AddTransient<PlaceBidPage>();
		builder.Services.AddTransient<PlaceBidViewModel>();

		builder.Services.AddTransient<PassedAuctionsViewModel>();

		// Admin Pages
		builder.Services.AddTransient<CategoryManagementPage>();
		builder.Services.AddTransient<CategoryManagementViewModel>();
		builder.Services.AddTransient<CategoryDetailPage>();
		builder.Services.AddTransient<CategoryDetailViewModel>();
		builder.Services.AddTransient<AdminCategorySkusPage>();
		builder.Services.AddTransient<AdminCategorySkusViewModel>();

		builder.Services.AddTransient<AdminJobReviewPage>();
		builder.Services.AddTransient<AdminJobReviewViewModel>();

		builder.Services.AddTransient<UserManagementPage>();
		builder.Services.AddTransient<UserManagementViewModel>();
		builder.Services.AddTransient<UserEditPage>();
		builder.Services.AddTransient<UserEditViewModel>();
		builder.Services.AddTransient<AdminProjectsViewModel>();

		Routing.RegisterRoute(nameof(CategoryManagementPage), typeof(CategoryManagementPage));
		Routing.RegisterRoute(nameof(Views.Admin.UserManagementPage), typeof(Views.Admin.UserManagementPage));
		Routing.RegisterRoute(nameof(Views.Admin.AdminJobReviewPage), typeof(Views.Admin.AdminJobReviewPage));

		Routing.RegisterRoute(nameof(CreateAccountPage), typeof(CreateAccountPage));
		Routing.RegisterRoute(nameof(BookingPage), typeof(BookingPage));
		Routing.RegisterRoute(nameof(CategoryDetailPage), typeof(CategoryDetailPage));
		Routing.RegisterRoute(nameof(AdminCategorySkusPage), typeof(AdminCategorySkusPage));
		Routing.RegisterRoute(nameof(UserEditPage), typeof(UserEditPage));
		Routing.RegisterRoute(nameof(ScopeReviewPage), typeof(ScopeReviewPage));
		Routing.RegisterRoute(nameof(AuctionHubPage), typeof(AuctionHubPage));
		Routing.RegisterRoute(nameof(TaskBreakdownPage), typeof(TaskBreakdownPage));
		Routing.RegisterRoute(nameof(BidDetailsPage), typeof(BidDetailsPage));
		Routing.RegisterRoute(nameof(PlaceBidPage), typeof(PlaceBidPage));
		Routing.RegisterRoute(nameof(TradesmanBookingDashboardPage), typeof(TradesmanBookingDashboardPage));
		Routing.RegisterRoute(nameof(CheckoutPage), typeof(CheckoutPage));
		Routing.RegisterRoute(nameof(BookingDashboardPage), typeof(BookingDashboardPage));
		Routing.RegisterRoute(nameof(BlazorHostPage), typeof(BlazorHostPage));

		builder.Logging.AddDebug();

		var app = builder.Build();

		AppServiceLocator.Navigation = app.Services.GetRequiredService<INavigationBridge>();
		AppServiceLocator.Alerts = app.Services.GetRequiredService<IAlertService>();
		AppServiceLocator.MainThread = app.Services.GetRequiredService<IAppMainThread>();

		return app;
	}
};