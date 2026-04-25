namespace BuildSmart.Maui.Services;

public class NavigationBridge : INavigationBridge
{
    public Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (parameters != null)
            {
                await Shell.Current.GoToAsync(route, parameters);
            }
            else
            {
                await Shell.Current.GoToAsync(route);
            }
        });
        
        return Task.CompletedTask;
    }

    public Task GoBackAsync()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Shell.Current.GoToAsync("..");
        });

        return Task.CompletedTask;
    }
}