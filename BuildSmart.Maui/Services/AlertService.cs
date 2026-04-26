using BuildSmart.SharedUI.Services;

namespace BuildSmart.Maui.Services;

public class AlertService : IAlertService
{
	private Page? GetCurrentPage() => Application.Current?.Windows.FirstOrDefault()?.Page ?? Application.Current?.MainPage;

	public Task DisplayAlert(string title, string message, string cancel)
	{
		return MainThread.InvokeOnMainThreadAsync(() =>
			GetCurrentPage()?.DisplayAlert(title, message, cancel) ?? Task.CompletedTask);
	}

	public Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
	{
		return MainThread.InvokeOnMainThreadAsync(() =>
			GetCurrentPage()?.DisplayAlert(title, message, accept, cancel) ?? Task.FromResult(false));
	}

	public Task<string> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string placeholder = null, int maxLength = -1, object keyboard = null, string initialValue = "")
	{
		return MainThread.InvokeOnMainThreadAsync(() =>
			GetCurrentPage()?.DisplayPromptAsync(title, message, accept, cancel, placeholder, maxLength, (Keyboard?)keyboard, initialValue) ?? Task.FromResult(string.Empty));
	}
}