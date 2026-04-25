namespace BuildSmart.Maui.Services;

public interface INavigationBridge
{
    Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null);
    Task GoBackAsync();
}
