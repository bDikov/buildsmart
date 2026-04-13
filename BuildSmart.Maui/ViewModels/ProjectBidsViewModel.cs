using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.Maui.ViewModels;

public partial class ProjectBidsViewModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty]
    private IJobPostDetails? _job;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Job", out var jobObj) && jobObj is IJobPostDetails job)
        {
            Job = job;
        }
    }

    [RelayCommand]
    private async Task ViewBidDetailsAsync(IGetProjectsForReview_ProjectsForReview_JobPosts_Bids bid)
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(Views.BidDetailsPage), new Dictionary<string, object>
            {
                { "Bid", bid },
                { "Job", Job! }
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Navigation Error", ex.Message, "OK");
        }
    }
}
