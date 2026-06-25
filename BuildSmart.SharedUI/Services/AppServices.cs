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
        private static INavigationBridge _navigation = null!;
        private static IAlertService _alerts = null!;
        private static IAppMainThread _mainThread = null!;

        public static Func<Type, object?>? ServiceResolver { get; set; }

        public static Func<string, string, Task>? ToastAction { get; set; }

        public static INavigationBridge Navigation
        {
            get => (INavigationBridge?)ServiceResolver?.Invoke(typeof(INavigationBridge)) ?? _navigation;
            set => _navigation = value;
        }

        public static IAlertService Alerts
        {
            get => (IAlertService?)ServiceResolver?.Invoke(typeof(IAlertService)) ?? _alerts;
            set => _alerts = value;
        }

        public static IAppMainThread MainThread
        {
            get => (IAppMainThread?)ServiceResolver?.Invoke(typeof(IAppMainThread)) ?? _mainThread;
            set => _mainThread = value;
        }
    }
}