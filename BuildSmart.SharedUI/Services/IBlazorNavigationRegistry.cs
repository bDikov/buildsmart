using Microsoft.AspNetCore.Components;

namespace BuildSmart.SharedUI.Services;

public interface IBlazorNavigationRegistry
{
    NavigationManager? CurrentManager { get; set; }
}

public class BlazorNavigationRegistry : IBlazorNavigationRegistry
{
    public NavigationManager? CurrentManager { get; set; }
}
