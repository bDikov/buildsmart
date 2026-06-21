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

                if (blazorUrl == "..")
                {
                    await _blazorNavigationRegistry.GoBackAsync();
                    return;
                }

                // Parse query string if present inside blazorUrl to perform EndsWith("Page") check correctly
                string pathPart = blazorUrl;
                string queryPart = "";
                int qIndex = blazorUrl.IndexOf('?');
                if (qIndex >= 0)
                {
                    pathPart = blazorUrl.Substring(0, qIndex);
                    queryPart = blazorUrl.Substring(qIndex);
                }

                // Map old Page-based routes to new kebab-case routes
                if (pathPart.EndsWith("Page") && !pathPart.StartsWith("//"))
                {
                    var pageName = pathPart.Replace("Page", "");
                    var mappedPath = pageName switch
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
                        "CategoryManagement" => "/category-management",
                        "CategoryDetail" => "/category-detail",
                        "AdminCategorySkus" => "/admin-category-skus",
                        _ => pathPart
                    };
                    blazorUrl = mappedPath + queryPart;
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
            if (_blazorNavigationRegistry.GoBackAction != null)
            {
                await _blazorNavigationRegistry.GoBackAsync();
            }
            else
            {
                await Shell.Current.GoToAsync("..");
            }
        });

        return Task.CompletedTask;
    }
}

