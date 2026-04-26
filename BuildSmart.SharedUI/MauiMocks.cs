using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace BuildSmart.SharedUI.MauiMocks
{
    public interface IQueryAttributable
    {
        void ApplyQueryAttributes(IDictionary<string, object> query);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class QueryPropertyAttribute : Attribute
    {
        public QueryPropertyAttribute(string name, string queryId) { }
    }

    public static class MainThread
    {
        public static void BeginInvokeOnMainThread(Action action) => Task.Run(action);
        public static Task InvokeOnMainThreadAsync(Action action) { action(); return Task.CompletedTask; }
        public static Task InvokeOnMainThreadAsync(Func<Task> func) => func();
    }

    public class Application
    {
        public static Application Current { get; } = new Application();
        public MainPage MainPage { get; set; } = new MainPage();
    }

    public class MainPage
    {
        public Task DisplayAlert(string title, string message, string cancel) => Task.CompletedTask;
    }

    public interface IMediaPicker
    {
        Task<FileResult?> PickPhotoAsync(MediaPickerOptions? options = null);
        Task<FileResult?> PickVideoAsync(MediaPickerOptions? options = null);
    }

    public class MediaPickerOptions
    {
        public string Title { get; set; } = string.Empty;
    }

    public class FileResult
    {
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public Func<Task<Stream>> StreamFunc { get; set; } = () => Task.FromResult(Stream.Null);
        public Task<Stream> OpenReadAsync() => StreamFunc();
    }

    public interface IFilePicker
    {
        Task<FileResult?> PickAsync(PickOptions? options = null);
    }

    public static class DeviceInfo
    {
        public static DeviceInfoInstance Current { get; } = new DeviceInfoInstance();
        public static DevicePlatform Platform => DevicePlatform.Web;
    }

    public class DeviceInfoInstance
    {
        public DevicePlatform Platform => DevicePlatform.Web;
    }

    public class DevicePlatform
    {
        public static DevicePlatform Web { get; } = new DevicePlatform("Web");
        public static DevicePlatform Android { get; } = new DevicePlatform("Android");
        public static DevicePlatform iOS { get; } = new DevicePlatform("iOS");
        public static DevicePlatform MacCatalyst { get; } = new DevicePlatform("MacCatalyst");
        public static DevicePlatform WinUI { get; } = new DevicePlatform("WinUI");

        private string Name { get; }
        private DevicePlatform(string name) { Name = name; }
        public override string ToString() => Name;
    }

    public static class WebAuthenticator
    {
        public static IWebAuthenticator Default { get; set; } = null!;
    }

    public interface IWebAuthenticator
    {
        Task<WebAuthenticatorResult?> AuthenticateAsync(Uri url, Uri callbackUrl);
    }

    public class WebAuthenticatorResult
    {
        public string? AccessToken { get; set; }
    }

    public class PickOptions
    {
        public string PickerTitle { get; set; } = string.Empty;
    }
}

