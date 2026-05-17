using BuildSmart.SharedUI.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.SharedUI.ViewModels;

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
    public async Task NavigateToNotificationAsync(IGetMyNotifications_MyNotifications note)
    {
        // First mark as read
        await MarkAsReadAsync(note);

        if (!note.RelatedEntityId.HasValue || string.IsNullOrEmpty(note.RelatedEntityType))
        {
            return;
        }

        try
        {
            string route = string.Empty;
            
            // Map entity types to routes based on application logic
            switch (note.RelatedEntityType.ToLowerInvariant())
            {
                case "project":
                    route = $"/project-detail?projectId={note.RelatedEntityId.Value}";
                    break;
                case "jobpost":
                case "job":
                    route = $"/project-detail?jobId={note.RelatedEntityId.Value}";
                    break;
                case "bid":
                    // Assuming we navigate to project details and it handles the bid via API
                    // Or navigate to a specific bid page if it exists
                    route = $"/project-detail?bidId={note.RelatedEntityId.Value}";
                    break;
                case "booking":
                    route = $"/booking-detail?bookingId={note.RelatedEntityId.Value}"; // Update if specific route exists
                    break;
                case "tradesman":
                case "user":
                    route = $"/tradesman-details?tradesmanId={note.RelatedEntityId.Value}";
                    break;
                default:
                    break;
            }

            if (!string.IsNullOrEmpty(route))
            {
                await BuildSmart.SharedUI.Services.AppServiceLocator.Navigation.NavigateToAsync(route);
            }
        }
        catch { /* Navigation failed */ }
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
            bool isFirstLoad = Notifications.Count == 0;
            if (isFirstLoad) IsBusy = true;
            
            var result = await _apiClient.GetMyNotifications.ExecuteAsync();

            if (result.Errors.Count > 0) return;

            if (result.Data?.MyNotifications != null)
            {
                var newNotes = result.Data.MyNotifications.OrderByDescending(n => n.CreatedAt).ToList();
                Notifications = new ObservableCollection<IGetMyNotifications_MyNotifications>(newNotes);
            }
            else 
            {
                Notifications = new ObservableCollection<IGetMyNotifications_MyNotifications>();
            }
        }
        catch { /* Silently fail */ }
        finally
        {
            IsBusy = false;
        }
    }
}


