using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.Maui.ViewModels;

public partial class NotificationsViewModel : ObservableObject
{
    private readonly IBuildSmartApiClient _apiClient;

    public NotificationsViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [ObservableProperty]
    private ObservableCollection<IGetMyNotifications_MyNotifications> _notifications = new();

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    public async Task DeleteAllAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _apiClient.DeleteAllNotifications.ExecuteAsync();
            if (result.Errors.Count == 0)
            {
                Notifications.Clear();
            }
        }
        catch { }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task MarkAsReadAsync(IGetMyNotifications_MyNotifications note)
    {
        if (note.IsRead) return;

        try
        {
            await _apiClient.MarkNotificationAsRead.ExecuteAsync(note.Id);
            // Local update for immediate feedback
            var local = Notifications.FirstOrDefault(n => n.Id == note.Id);
            if (local != null)
            {
                // Note: StrawberryShake models might be immutable, so we might need to reload 
                // or use a wrapper class. For now, let's just reload.
                await LoadNotificationsAsync();
            }
        }
        catch { }
    }

    [RelayCommand]
    public async Task LoadNotificationsAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _apiClient.GetMyNotifications.ExecuteAsync();

            if (result.Errors.Count > 0) return;

            Notifications.Clear();
            if (result.Data?.MyNotifications != null)
            {
                foreach (var note in result.Data.MyNotifications.OrderByDescending(n => n.CreatedAt))
                {
                    Notifications.Add(note);
                }
            }
        }
        catch { /* Silently fail */ }
        finally
        {
            IsBusy = false;
        }
    }
}
