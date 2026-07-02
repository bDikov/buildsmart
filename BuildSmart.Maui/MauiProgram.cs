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
		// Initialize thread culture from MAUI Preferences on startup
		try
		{
			var lang = Microsoft.Maui.Storage.Preferences.Default.Get<string>("preferred_language", "bg");
			var culture = new System.Globalization.CultureInfo(lang);
			System.Globalization.CultureInfo.CurrentCulture = culture;
			System.Globalization.CultureInfo.CurrentUICulture = culture;
			System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
			System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[MauiProgram] Failed to initialize culture on startup: {ex.Message}");
		}

		// Configure SharedUI API Config based on MAUI platform
#if DEBUG
		if (Microsoft.Maui.Devices.DeviceInfo.Current.Platform == Microsoft.Maui.Devices.DevicePlatform.Android)
		{
			BuildSmart.SharedUI.ApiConfig.BaseUrlOverride = "https://10.0.2.2:7212";
		}
		else
		{
			BuildSmart.SharedUI.ApiConfig.BaseUrlOverride = "https://localhost:7212";
		}
#else
		BuildSmart.SharedUI.ApiConfig.BaseUrlOverride = "https://buildsmart.bg";
#endif

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseSentry(options => 
			{
				// The DSN is now read from ApiConfig which can be injected during build
				options.Dsn = ApiConfig.SentryDsn;

				// When debug is enabled, the Sentry SDK will emit diagnostic information to the logcat on Android or the console on iOS/Windows.
				options.Debug = false;

				// The percentage of HTTP requests to trace (1.0 = 100%)
				options.TracesSampleRate = 1.0;
				
				// Enable Sentry logging
				options.EnableLogs = true;
			})
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
		builder.Services.AddSingleton<BuildSmart.Core.Application.Interfaces.ILocalizationCacheService, BuildSmart.SharedUI.Services.Localization.LocalizationCacheService>();
		builder.Services.AddSingleton<Microsoft.Extensions.Localization.IStringLocalizerFactory, BuildSmart.SharedUI.Services.Localization.DbStringLocalizerFactory>();
		builder.Services.AddSingleton<BuildSmart.SharedUI.Services.ILocalizationStateService, BuildSmart.SharedUI.Services.LocalizationStateService>();

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

		builder.Services.AddHttpClient<IQuestionManagementApiClient, QuestionManagementApiClient>()
			.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
			{
				ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
			})
			.AddHttpMessageHandler<AuthHeaderHandler>();

		builder.Services.AddSingleton<LoginPage>(); builder.Services.AddSingleton<LoginPageViewModel>();
		builder.Services.AddTransient<DetailedViewPageViewModel>();
		builder.Services.AddTransient<CreateAccountPage>();
		builder.Services.AddTransient<CreateAccountPageViewModel>();

		builder.Services.AddTransient<FeedPage>();
		builder.Services.AddScoped<FeedPageViewModel>();

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


		builder.Services.AddTransient<AdminJobReviewPage>();
		builder.Services.AddTransient<AdminJobReviewViewModel>();

		builder.Services.AddTransient<UserManagementPage>();
		builder.Services.AddTransient<UserManagementViewModel>();
		builder.Services.AddTransient<UserEditPage>();
		builder.Services.AddTransient<UserEditViewModel>();
		builder.Services.AddTransient<AdminProjectsViewModel>();

		Routing.RegisterRoute(nameof(Views.Admin.UserManagementPage), typeof(Views.Admin.UserManagementPage));
		Routing.RegisterRoute(nameof(Views.Admin.AdminJobReviewPage), typeof(Views.Admin.AdminJobReviewPage));

		Routing.RegisterRoute(nameof(CreateAccountPage), typeof(CreateAccountPage));
		Routing.RegisterRoute(nameof(BookingPage), typeof(BookingPage));
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

		// Initialize and Sync MAUI Localization Cache
		try
		{
			var cacheService = app.Services.GetRequiredService<BuildSmart.Core.Application.Interfaces.ILocalizationCacheService>();
			var stateService = app.Services.GetRequiredService<BuildSmart.SharedUI.Services.ILocalizationStateService>();
			var apiClient = app.Services.GetRequiredService<BuildSmart.SharedUI.GraphQL.IBuildSmartApiClient>();

			// 1. Load from Preferences instantly on startup to avoid flickering
			var cachedEnJson = Microsoft.Maui.Storage.Preferences.Default.Get<string?>("localization_cache_en", null);
			var cachedBgJson = Microsoft.Maui.Storage.Preferences.Default.Get<string?>("localization_cache_bg", null);
			
			if (!string.IsNullOrEmpty(cachedEnJson) && !string.IsNullOrEmpty(cachedBgJson))
			{
				try
				{
					var enDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(cachedEnJson);
					var bgDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(cachedBgJson);
					if (enDict != null && bgDict != null)
					{
						var initialData = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
						{
							{ "en", enDict },
							{ "bg", bgDict }
						};
						cacheService.Initialize(initialData);
						Console.WriteLine("MAUI Localization cache initialized from Preferences.");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error deserializing cached translations: {ex.Message}");
				}
			}

			// 2. Background Sync from GraphQL API
			_ = Task.Run(async () =>
			{
				// Wait a moment for app initialization
				await Task.Delay(2000);

				if (Microsoft.Maui.Networking.Connectivity.Current.NetworkAccess != Microsoft.Maui.Networking.NetworkAccess.Internet)
				{
					Console.WriteLine("No internet connection. Skipping localization sync.");
					return;
				}

				int retries = 0;
				while (retries < 5)
				{
					try
					{
						var result = await apiClient.GetLocalizationStrings.ExecuteAsync("en");
						var bgResult = await apiClient.GetLocalizationStrings.ExecuteAsync("bg");

						if (result.Data?.LocalizationStrings != null && bgResult.Data?.LocalizationStrings != null)
						{
							var enDict = result.Data.LocalizationStrings.ToDictionary(r => r.Key, r => r.Value, StringComparer.OrdinalIgnoreCase);
							var bgDict = bgResult.Data.LocalizationStrings.ToDictionary(r => r.Key, r => r.Value, StringComparer.OrdinalIgnoreCase);

							var freshData = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
							{
								{ "en", enDict },
								{ "bg", bgDict }
							};

							cacheService.Initialize(freshData);

							// Save to local Preferences
							var enJson = System.Text.Json.JsonSerializer.Serialize(enDict);
							var bgJson = System.Text.Json.JsonSerializer.Serialize(bgDict);
							Microsoft.Maui.Storage.Preferences.Default.Set("localization_cache_en", enJson);
							Microsoft.Maui.Storage.Preferences.Default.Set("localization_cache_bg", bgJson);

							// Notify UI to re-render Blazor components
							stateService.NotifyLocalizationChanged();
							Console.WriteLine("MAUI Localization cache synced successfully from API.");
							break;
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Retrying MAUI localization sync: {ex.Message}");
						await Task.Delay(5000);
						retries++;
					}
				}
			});
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error during MAUI localization cache setup: {ex.Message}");
		}

		return app;
	}
};