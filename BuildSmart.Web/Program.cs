using BuildSmart.Web.Components;
using BuildSmart.Web.Services;
using BuildSmart.SharedUI;
using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.Handlers;
using BuildSmart.SharedUI.MauiMocks;
using Microsoft.AspNetCore.Components.Authorization;
using BuildSmart.SharedUI.ViewModels;
using BuildSmart.SharedUI.ViewModels.Admin;
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

// Configure SharedUI API Config based on Web
BuildSmart.SharedUI.ApiConfig.BaseUrlOverride = "https://localhost:7212";

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
builder.Services.AddScoped<SignalRService>();
builder.Services.AddScoped<IFileService, FileService>();

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

// ViewModels
builder.Services.AddTransient<LoginPageViewModel>();
builder.Services.AddTransient<DetailedViewPageViewModel>();
builder.Services.AddTransient<CreateAccountPageViewModel>();
builder.Services.AddTransient<FeedPageViewModel>();
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
builder.Services.AddTransient<CategoryManagementViewModel>();
builder.Services.AddTransient<CategoryDetailViewModel>();
builder.Services.AddTransient<AdminCategorySkusViewModel>();
builder.Services.AddTransient<AdminJobReviewViewModel>();
builder.Services.AddTransient<UserManagementViewModel>();
builder.Services.AddTransient<UserEditViewModel>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

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

app.Run();
