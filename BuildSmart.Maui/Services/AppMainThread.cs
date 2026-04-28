using BuildSmart.SharedUI.Services;

namespace BuildSmart.Maui.Services;

public class AppMainThread : IAppMainThread
{
    public void BeginInvokeOnMainThread(Action action)
    {
        MainThread.BeginInvokeOnMainThread(action);
    }

    public Task InvokeOnMainThreadAsync(Action action)
    {
        return MainThread.InvokeOnMainThreadAsync(action);
    }

    public Task InvokeOnMainThreadAsync(Func<Task> func)
    {
        return MainThread.InvokeOnMainThreadAsync(func);
    }
}
