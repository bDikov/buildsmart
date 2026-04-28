using BuildSmart.SharedUI.Services;
using Microsoft.AspNetCore.Components;

namespace BuildSmart.Maui.Services;

public class NavigationBridge : INavigationBridge
{
    private readonly IBlazorNavigationRegistry _blazorNavigationRegistry;

    public NavigationBridge(IBlazorNavigationRegistry blazorNavigationRegistry)
    {
        _blazorNavigationRegistry = blazorNavigationRegistry;
    }

    public Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var url = route;

            // 1. Check if it's a Blazor route (starts with / or is a known mapped route)
            // If we have a Blazor NavigationManager registered, we prefer using it for Blazor routes.
            if (_blazorNavigationRegistry.CurrentManager != null)
            {
                var blazorUrl = url;

                // Map old Page-based routes to new kebab-case routes
                if (blazorUrl.EndsWith("Page") && !blazorUrl.StartsWith("//"))
                {
                    var pageName = blazorUrl.Replace("Page", "");
                    blazorUrl = pageName switch
                    {
                        "JobWizard" => "/job-wizard",
                        "ProjectDetail" => "/project-detail",
                        "PassedAuctions" => "/passed-auctions",
                        "Notifications" => "/notifications",
                        "ActiveJobs" => "/active-jobs",
                        "TradesmanDetails" => "/tradesman-details",
                        "AuctionHub" => "/auction-hub",
                        "ScopeReview" => "/scope-review",
                        "TaskBreakdown" => "/task-breakdown",
                        "BidDetails" => "/bid-details",
                        "PlaceBid" => "/place-bid",
                        "TradesmanBookingDashboard" => "/tradesman-booking-dashboard",
                        "Checkout" => "/checkout",
                        "BookingDashboard" => "/booking-dashboard",
                        _ => blazorUrl
                    };
                }

                if (blazorUrl.StartsWith("/") && !blazorUrl.StartsWith("//"))
                {
                    if (parameters != null && parameters.Count > 0)
                    {
                        var separator = blazorUrl.Contains("?") ? "&" : "?";
                        var queryString = string.Join("&", parameters.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value?.ToString() ?? string.Empty)}"));
                        blazorUrl = $"{blazorUrl}{separator}{queryString}";
                    }

                    _blazorNavigationRegistry.CurrentManager.NavigateTo(blazorUrl);
                    return;
                }
            }

            // 2. Fallback to native Shell navigation
            if (parameters != null)
            {
                await Shell.Current.GoToAsync(url, parameters);
            }
            else
            {
                await Shell.Current.GoToAsync(url);
            }
        });
        
        return Task.CompletedTask;
    }

    public Task GoBackAsync()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (_blazorNavigationRegistry.CurrentManager != null)
            {
                // Note: Blazor NavigationManager doesn't have a direct "GoBack", 
                // but we can try to use JS Interop or just stick to Shell for physical back button feel
                // However, for consistency with the bridge's current behavior:
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.GoToAsync("..");
            }
        });

        return Task.CompletedTask;
    }
}

