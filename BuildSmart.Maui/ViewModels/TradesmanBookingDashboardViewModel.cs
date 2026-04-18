using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.Maui.ViewModels;

[QueryProperty(nameof(BookingId), "bookingId")]
public partial class TradesmanBookingDashboardViewModel : ObservableObject, IQueryAttributable
{
    private readonly IBuildSmartApiClient _apiClient;

    public TradesmanBookingDashboardViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [ObservableProperty]
    private string? _bookingId;

    [ObservableProperty]
    private IGetMyActiveBookings_MyActiveBookings? _booking;

    public ObservableCollection<IGetMyActiveBookings_MyActiveBookings_MilestonePayments> MilestonePayments { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("bookingId", out var idObj) && idObj != null)
        {
            BookingId = idObj.ToString();
            MainThread.BeginInvokeOnMainThread(async () => await InitializeAsync());
        }
    }

    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(BookingId)) return;
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            MilestonePayments.Clear();

            var result = await _apiClient.GetMyActiveBookings.ExecuteAsync();

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            if (result.Data?.MyActiveBookings != null)
            {
                Booking = result.Data.MyActiveBookings.FirstOrDefault(b => b.Id == BookingId);
                
                if (Booking != null)
                {
                    foreach (var milestone in Booking.MilestonePayments)
                    {
                        MilestonePayments.Add(milestone);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("System Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
