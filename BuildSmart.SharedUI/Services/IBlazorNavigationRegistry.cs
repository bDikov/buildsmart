using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace BuildSmart.SharedUI.Services;

public interface IBlazorNavigationRegistry
{
    NavigationManager? CurrentManager { get; set; }
    Func<Task>? GoBackAction { get; set; }
    Task GoBackAsync();
}

public class BlazorNavigationRegistry : IBlazorNavigationRegistry
{
    public NavigationManager? CurrentManager { get; set; }
    public Func<Task>? GoBackAction { get; set; }

    public async Task GoBackAsync()
    {
        if (GoBackAction != null)
        {
            await GoBackAction.Invoke();
        }
    }
}
