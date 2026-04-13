using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuildSmart.Maui.ViewModels;

public partial class BidDetailsViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty]
    private IGetProjectsForReview_ProjectsForReview_JobPosts_Bids? _bid;

    [ObservableProperty]
    private IJobPostDetails? _job;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Job", out var jobObj) && jobObj is IJobPostDetails job)
        {
            Job = job;
        }

        if (query.TryGetValue("Bid", out var bidObj) && bidObj is IGetProjectsForReview_ProjectsForReview_JobPosts_Bids bid)
        {
            Bid = bid;
        }
    }

    [RelayCommand]
    private async Task ProceedToCheckoutAsync()
    {
        if (Bid == null) return;
        
        try
        {
            await Shell.Current.GoToAsync(nameof(Views.CheckoutPage), new Dictionary<string, object>
            {
                { "Bid", Bid }
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Navigation Error", ex.Message, "OK");
        }
    }
}
