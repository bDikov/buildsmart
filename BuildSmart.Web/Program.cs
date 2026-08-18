using BuildSmart.Web.Components;
using BuildSmart.Web.Services;
using BuildSmart.SharedUI;
using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.Handlers;
using BuildSmart.SharedUI.MauiMocks;
using Microsoft.AspNetCore.Components.Authorization;
using BuildSmart.SharedUI.ViewModels;
using BuildSmart.SharedUI.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;
using System.IO;

AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
{
    if (eventArgs.Exception is OperationCanceledException || eventArgs.Exception is TaskCanceledException) return;
    if (eventArgs.Exception.Message.Contains("The request was canceled")) return;
    
    // Filter out typical EF/Cancellation noise to keep the log clean
    if (eventArgs.Exception.StackTrace?.Contains("Microsoft.AspNetCore.SignalR") == true ||
        eventArgs.Exception.StackTrace?.Contains("BuildSmart") == true)
    {
        try
        {
            File.AppendAllText("blazor_crash.log", $"[{DateTime.Now}] {eventArgs.Exception.GetType().Name}: {eventArgs.Exception.Message}\n{eventArgs.Exception.StackTrace}\n\n");
        }
        catch { }
    }
};

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = true;
    })
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 102400000;
    });

builder.Services.AddSignalR(options => 
{
    options.MaximumReceiveMessageSize = 102400000;
});

// Add HttpContextAccessor for reading cookies in WebAuthService
builder.Services.AddHttpContextAccessor();

builder.Services.AddLocalization();
builder.Services.AddSingleton<BuildSmart.Core.Application.Interfaces.ILocalizationCacheService, BuildSmart.SharedUI.Services.Localization.LocalizationCacheService>();
builder.Services.AddSingleton<Microsoft.Extensions.Localization.IStringLocalizerFactory, BuildSmart.SharedUI.Services.Localization.DbStringLocalizerFactory>();
builder.Services.AddScoped<BuildSmart.SharedUI.Services.ILocalizationStateService, BuildSmart.SharedUI.Services.LocalizationStateService>();
builder.Services.AddScoped<BuildSmart.Core.Application.Interfaces.IMediaService, BuildSmart.Infrastructure.Services.CloudflareR2MediaService>();

var webConnString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(webConnString))
{
    builder.Services.AddDbContextFactory<BuildSmart.Infrastructure.Persistence.AppDbContext>(options =>
        options.UseNpgsql(webConnString));
}
else
{
    var inMemoryDbName = "BuildSmartWeb_Shared";
    builder.Services.AddDbContextFactory<BuildSmart.Infrastructure.Persistence.AppDbContext>(options =>
        options.UseInMemoryDatabase(inMemoryDbName));
}

// Configure SharedUI API Config based on Web
var apiUrl = builder.Configuration["ApiConfig:BaseUrlOverride"] ?? builder.Configuration["ApiConfig:BaseUrl"];
BuildSmart.SharedUI.ApiConfig.BaseUrlOverride = !string.IsNullOrEmpty(apiUrl) ? apiUrl : "https://localhost:7212";
BuildSmart.SharedUI.ApiConfig.ClarityProjectId = builder.Configuration["Clarity:ProjectId"];
BuildSmart.SharedUI.ApiConfig.GoogleTagManagerId = builder.Configuration["GoogleTagManager:Id"];
BuildSmart.SharedUI.ApiConfig.PostHogApiKey = builder.Configuration["PostHog:ApiKey"];
var postHogHost = builder.Configuration["PostHog:ApiHost"];
if (!string.IsNullOrEmpty(postHogHost))
{
    BuildSmart.SharedUI.ApiConfig.PostHogApiHost = postHogHost;
}

// Web-specific mocks
builder.Services.AddSingleton<IMediaPicker, WebMediaPicker>();
builder.Services.AddSingleton<IFilePicker, WebFilePicker>();

// Web-specific Services
builder.Services.AddScoped<IAuthService, WebAuthService>();
builder.Services.AddScoped<IAlertService, WebAlertService>();
builder.Services.AddScoped<IBlazorNavigationRegistry, BlazorNavigationRegistry>();
builder.Services.AddScoped<INavigationBridge, WebNavigationBridge>();
builder.Services.AddScoped<IAppMainThread, WebAppMainThread>();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler, CircuitContextHandler>();

// Shared Services
builder.Services.AddScoped<IAdminPerspectiveService, AdminPerspectiveService>();
builder.Services.AddScoped<SignalRService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<BuildSmart.Core.Application.Interfaces.IEmailVerificationService, BuildSmart.Infrastructure.Services.EmailVerificationService>();
builder.Services.AddScoped<BuildSmart.Core.Application.Interfaces.IEmailService, BuildSmart.Infrastructure.Services.EmailService>();
builder.Services.AddScoped<BuildSmart.Core.Application.Interfaces.ICalculatorLeadRepository, BuildSmart.Infrastructure.Persistence.Repositories.CalculatorLeadRepository>();
builder.Services.AddScoped<BuildSmart.Core.Application.Interfaces.IProjectManagementService, BuildSmart.Infrastructure.Services.ProjectManagementService>();

builder.Services.AddHttpClient();
builder.Services.AddTransient<AuthHeaderHandler>();
builder.Services.AddTransient<LoggingHandler>();

// Blazor Authentication & Authorization
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<AuthenticationStateProvider, MauiAuthenticationStateProvider>();

// Register Strawberry Shake with fluent configuration
builder.Services.AddBuildSmartApiClient()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri(ApiConfig.GetGraphQLUrl());
        client.DefaultRequestVersion = System.Net.HttpVersion.Version11;
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
    }, builder =>
    {
        builder.ConfigurePrimaryHttpMessageHandler(() => {
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

// ViewModels
builder.Services.AddTransient<LoginPageViewModel>();
builder.Services.AddTransient<DetailedViewPageViewModel>();
builder.Services.AddTransient<CreateAccountPageViewModel>();
builder.Services.AddTransient<VerifyEmailPageViewModel>();
builder.Services.AddScoped<FeedPageViewModel>();
builder.Services.AddTransient<TradesmanDetailsViewModel>();
builder.Services.AddTransient<BookingPageViewModel>();
builder.Services.AddTransient<JobWizardViewModel>();
builder.Services.AddTransient<UserProfileViewModel>();
builder.Services.AddTransient<MyProjectsViewModel>();
builder.Services.AddTransient<ProjectDetailViewModel>();
builder.Services.AddTransient<ScopeReviewViewModel>();
builder.Services.AddTransient<GeneratedOfferViewModel>();
builder.Services.AddTransient<NotificationsViewModel>();
builder.Services.AddTransient<AuctionHubViewModel>();
builder.Services.AddTransient<TaskBreakdownViewModel>();
builder.Services.AddTransient<BidDetailsViewModel>();
builder.Services.AddTransient<CheckoutViewModel>();
builder.Services.AddTransient<BookingDashboardViewModel>();
builder.Services.AddTransient<ActiveJobsViewModel>();
builder.Services.AddTransient<TradesmanBookingDashboardViewModel>();
builder.Services.AddTransient<PlaceBidViewModel>();
builder.Services.AddTransient<PassedAuctionsViewModel>();

// Admin ViewModels

builder.Services.AddTransient<AdminJobReviewViewModel>();
builder.Services.AddTransient<UserManagementViewModel>();
builder.Services.AddTransient<UserEditViewModel>();
builder.Services.AddTransient<AdminProjectsViewModel>();

var app = builder.Build();

// Configure the SharedUI AppServiceLocator to resolve scoped services from the active Blazor Server circuit context
BuildSmart.SharedUI.Services.AppServiceLocator.ServiceResolver = (type) =>
{
    try
    {
        var services = BuildSmart.Web.Services.BlazorCircuitContext.CurrentServices.Value;
        return services?.GetService(type);
    }
    catch (ObjectDisposedException)
    {
        return null;
    }
};

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

var supportedCultures = new[] { "bg", "en" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("bg")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

if (localizationOptions.SupportedCultures != null)
{
    foreach (var culture in localizationOptions.SupportedCultures)
    {
        culture.NumberFormat.CurrencySymbol = "€";
    }
}

app.UseRequestLocalization(localizationOptions);

app.UseAuthentication();

// Middleware to parse the raw JWT from the auth_token cookie so ASP.NET Core Endpoint Routing doesn't issue a 302 redirect
app.Use(async (context, next) =>
{
    if (context.Request.Cookies.TryGetValue("auth_token", out var token) && !string.IsNullOrEmpty(token))
    {
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            if (jwt.ValidTo > DateTime.UtcNow)
            {
                var roleClaimType = jwt.Claims.FirstOrDefault(c => c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Type ?? "role";
                var identity = new System.Security.Claims.ClaimsIdentity(jwt.Claims, "jwt", "name", roleClaimType);
                context.User = new System.Security.Claims.ClaimsPrincipal(identity);
            }
        }
        catch { /* Invalid or malformed token */ }
    }
    await next();
});

app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(BuildSmart.SharedUI.Components.Layout.MainLayout).Assembly);

app.MapGet("/sitemap.xml", async (HttpContext context) =>
{
    context.Response.ContentType = "application/xml";
    
    var sitemapXml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">
    <url>
        <loc>https://buildsmart.bg/</loc>
        <changefreq>daily</changefreq>
        <priority>1.0</priority>
    </url>
    <url>
        <loc>https://buildsmart.bg/feed</loc>
        <changefreq>daily</changefreq>
        <priority>0.9</priority>
    </url>
    <url>
        <loc>https://buildsmart.bg/faq</loc>
        <changefreq>weekly</changefreq>
        <priority>0.8</priority>
    </url>
    <url>
        <loc>https://buildsmart.bg/terms</loc>
        <changefreq>monthly</changefreq>
        <priority>0.3</priority>
    </url>
    <url>
        <loc>https://buildsmart.bg/privacy</loc>
        <changefreq>monthly</changefreq>
        <priority>0.3</priority>
    </url>
    <url>
        <loc>https://buildsmart.bg/request-consultation</loc>
        <changefreq>monthly</changefreq>
        <priority>0.7</priority>
    </url>
    <url>
        <loc>https://buildsmart.bg/blog</loc>
        <changefreq>daily</changefreq>
        <priority>0.9</priority>
    </url>
    <url>
        <loc>https://buildsmart.bg/blog/remont-na-apartament-sofia-cena-2026</loc>
        <changefreq>weekly</changefreq>
        <priority>0.9</priority>
    </url>
    <url>
        <loc>https://buildsmart.bg/blog/remont-na-banya-sofia-cena-2026</loc>
        <changefreq>weekly</changefreq>
        <priority>0.8</priority>
    </url>
    <url>
        <loc>https://buildsmart.bg/blog/suho-stroitelstvo-gipskarton-sofia-cena</loc>
        <changefreq>weekly</changefreq>
        <priority>0.8</priority>
    </url>
    <url>
        <loc>https://buildsmart.bg/blog/maistori-red-flags-sofia-dogovor</loc>
        <changefreq>weekly</changefreq>
        <priority>0.8</priority>
    </url>
    <url>
        <loc>https://buildsmart.bg/blog/remont-na-3-staen-apartament-realen-kazus</loc>
        <changefreq>weekly</changefreq>
        <priority>0.8</priority>
    </url>
</urlset>";
    
    await context.Response.WriteAsync(sitemapXml);
});

// Fetch and warm up localization cache from API
try
{
	var cacheService = app.Services.GetRequiredService<BuildSmart.Core.Application.Interfaces.ILocalizationCacheService>();
	var apiClient = app.Services.GetRequiredService<BuildSmart.SharedUI.GraphQL.IBuildSmartApiClient>();
	
	_ = Task.Run(async () =>
	{
		int retries = 0;
		while (retries < 15)
		{
			try
			{
				var result = await apiClient.GetLocalizationStrings.ExecuteAsync("en");
				var bgResult = await apiClient.GetLocalizationStrings.ExecuteAsync("bg");
				
				if (result.Data?.LocalizationStrings != null && bgResult.Data?.LocalizationStrings != null)
				{
					var cacheData = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
					
					var enDict = result.Data.LocalizationStrings.ToDictionary(r => r.Key, r => r.Value, StringComparer.OrdinalIgnoreCase);
					var bgDict = bgResult.Data.LocalizationStrings.ToDictionary(r => r.Key, r => r.Value, StringComparer.OrdinalIgnoreCase);
					
					cacheData["en"] = enDict;
					cacheData["bg"] = bgDict;
					
					cacheService.Initialize(cacheData);
					Console.WriteLine("Localization cache warmed up successfully from API.");
					break;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Retrying localization warm up from API: {ex.Message}");
				await Task.Delay(3000);
				retries++;
			}
		}
	});
}
catch (Exception ex)
{
	Console.WriteLine($"Error initializing localization warm up task: {ex.Message}");
}

try
{
	// Sync default blog images into wwwroot/images/blog if missing from volume
	var webImagesDir = Path.Combine(app.Environment.WebRootPath, "images", "blog");
	Directory.CreateDirectory(webImagesDir);
	var baseImagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "blog");
	if (Directory.Exists(baseImagesDir))
	{
		foreach (var file in Directory.GetFiles(baseImagesDir))
		{
			var destFile = Path.Combine(webImagesDir, Path.GetFileName(file));
			if (!File.Exists(destFile))
			{
				File.Copy(file, destFile, overwrite: false);
			}
		}
	}

	using var scope = app.Services.CreateScope();
	var dbFactory = scope.ServiceProvider.GetService<IDbContextFactory<BuildSmart.Infrastructure.Persistence.AppDbContext>>();
	if (dbFactory != null)
	{
		await using var db = await dbFactory.CreateDbContextAsync();
		await db.SeedBlogPostsAsync(app.Environment.WebRootPath);
	}
}
catch (Exception ex)
{
	Console.WriteLine($"Blog seeder note: {ex.Message}");
}

app.Run();
