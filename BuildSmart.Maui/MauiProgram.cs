using Microsoft.Extensions.Logging;
using BuildSmart.Maui.GraphQL; // Added
using StrawberryShake.Transport.WebSockets; // Added
using BuildSmart.Maui.Views;
using BuildSmart.Maui.ViewModels;
using BuildSmart.Maui.Services;
using BuildSmart.Maui.Handlers;
using BuildSmart.Maui.Views.Admin;
using BuildSmart.Maui.ViewModels.Admin;

namespace BuildSmart.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Configure StrawberryShake GraphQL Client

		builder.Services.AddSingleton<IAuthService, AuthService>();
		builder.Services.AddTransient<AuthHeaderHandler>();
		builder.Services.AddTransient<LoggingHandler>();

		// Register Strawberry Shake with fluent configuration
		builder.Services.AddBuildSmartApiClient()
			.ConfigureHttpClient(client =>
			{
				client.BaseAddress = new Uri(ApiConfig.GetGraphQLUrl());
				// Force HTTP/1.1 for local development compatibility
				client.DefaultRequestVersion = System.Net.HttpVersion.Version11;
				client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
			}, builder =>
			{
				builder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
				{
					ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
				})
				.AddHttpMessageHandler<LoggingHandler>()
				.AddHttpMessageHandler<AuthHeaderHandler>();
			});

		builder.Services.AddSingleton<LoginPage>(); builder.Services.AddSingleton<LoginPageViewModel>();
		builder.Services.AddTransient<DetailedViewPage>();
		builder.Services.AddTransient<DetailedViewPageViewModel>();
		builder.Services.AddTransient<CreateAccountPage>();
		builder.Services.AddTransient<CreateAccountPageViewModel>();

		builder.Services.AddSingleton<FeedPage>();
		builder.Services.AddSingleton<FeedPageViewModel>();

		builder.Services.AddTransient<TradesmanDetailsPage>();
		builder.Services.AddTransient<TradesmanDetailsViewModel>();

		builder.Services.AddTransient<BookingPage>();
		builder.Services.AddTransient<BookingPageViewModel>();

		builder.Services.AddTransient<JobWizardPage>();
		builder.Services.AddTransient<JobWizardViewModel>();

		// Admin Pages
		builder.Services.AddSingleton<AdminShell>();
		builder.Services.AddTransient<AppShell>();
		builder.Services.AddTransient<CategoryManagementPage>();
		builder.Services.AddTransient<CategoryManagementViewModel>();
		builder.Services.AddTransient<CategoryDetailPage>();
		builder.Services.AddTransient<CategoryDetailViewModel>();

		builder.Services.AddSingleton<MainShell>();

		Routing.RegisterRoute(nameof(DetailedViewPage), typeof(DetailedViewPage));
		Routing.RegisterRoute(nameof(CreateAccountPage), typeof(CreateAccountPage));
		Routing.RegisterRoute(nameof(FeedPage), typeof(FeedPage));
		Routing.RegisterRoute(nameof(TradesmanDetailsPage), typeof(TradesmanDetailsPage));
		Routing.RegisterRoute(nameof(BookingPage), typeof(BookingPage));
		Routing.RegisterRoute(nameof(JobWizardPage), typeof(JobWizardPage));
		Routing.RegisterRoute(nameof(CategoryManagementPage), typeof(CategoryManagementPage));
		Routing.RegisterRoute(nameof(CategoryDetailPage), typeof(CategoryDetailPage));
		builder.Logging.AddDebug();

		return builder.Build();
	}
}