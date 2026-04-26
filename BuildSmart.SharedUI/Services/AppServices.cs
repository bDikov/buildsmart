using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace BuildSmart.SharedUI.Services
{
    public interface IAlertService
    {
        Task DisplayAlert(string title, string message, string cancel);
        Task<bool> DisplayAlert(string title, string message, string accept, string cancel);
        Task<string> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string placeholder = null, int maxLength = -1, object keyboard = null, string initialValue = "");
    }

    public interface IAppMainThread
    {
        void BeginInvokeOnMainThread(Action action);
        Task InvokeOnMainThreadAsync(Action action);
        Task InvokeOnMainThreadAsync(Func<Task> func);
    }

    public static class AppServiceLocator
    {
        public static INavigationBridge Navigation { get; set; } = null!;
        public static IAlertService Alerts { get; set; } = null!;
        public static IAppMainThread MainThread { get; set; } = null!;
    }
}