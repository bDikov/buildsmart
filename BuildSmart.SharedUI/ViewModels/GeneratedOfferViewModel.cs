using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.MauiMocks;
using BuildSmart.SharedUI.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BuildSmart.SharedUI.ViewModels;

public partial class GeneratedOfferViewModel : ObservableObject, IQueryAttributable, IDisposable
{
    private readonly IBuildSmartApiClient _apiClient;
    private readonly SignalRService _signalRService;

    public GeneratedOfferViewModel(IBuildSmartApiClient apiClient, SignalRService signalRService)
    {
        _apiClient = apiClient;
        _signalRService = signalRService;

        _signalRService.NotificationReceived += OnNotificationReceived;
        // Ensure SignalR is connected so we don't miss the message
        _ = _signalRService.ConnectAsync();
    }

    private void OnNotificationReceived(string title, string message, object? data)
    {
        if (title == "Pricing Updated" && _jobId != Guid.Empty)
        {
            AppServiceLocator.MainThread.BeginInvokeOnMainThread(async () =>
            {
                await LoadOfferDetailsAsync(_jobId);
            });
        }
    }

    public void Dispose()
    {
        if (_signalRService != null)
        {
            _signalRService.NotificationReceived -= OnNotificationReceived;
        }
    }

    [ObservableProperty]
    private IJobPostDetails? _job;

    private Guid _jobId;

    [ObservableProperty]
    private decimal _totalEstimatedPrice;

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<IGetAiCalculationByJob_AiCalculationByJob_Tasks> Tasks { get; } = new();

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("JobId", out var jobIdObj) && Guid.TryParse(jobIdObj.ToString(), out var jobId))
        {
            _jobId = jobId;
            await LoadOfferDetailsAsync(jobId);
        }
        else if (query.TryGetValue("Job", out var jobObj) && jobObj is IJobPostDetails job)
        {
            Job = job;
            _jobId = job.Id;
            await LoadOfferDetailsAsync(job.Id);
        }
    }

    private async Task LoadOfferDetailsAsync(Guid jobId)
    {
        try
        {
            IsBusy = true;
            var result = await _apiClient.GetAiCalculationByJob.ExecuteAsync(jobId);
            var aiCalculation = result.Data?.AiCalculationByJob?.FirstOrDefault();

            Tasks.Clear();
            decimal total = 0;

            if (aiCalculation?.Tasks != null)
            {
                foreach (var task in aiCalculation.Tasks)
                {
                    Tasks.Add(task);
                    total += task.EstimatedPrice;
                }
            }

            TotalEstimatedPrice = total;
        }
        catch (Exception ex)
        {
            await AppServiceLocator.Alerts.DisplayAlert("Error", "Could not load offer details: " + ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SubmitToAdminAsync()
    {
        if (_jobId == Guid.Empty) return;
        
        try
        {
            IsBusy = true;
            var approveResult = await _apiClient.ApproveJobScope.ExecuteAsync(_jobId, string.Empty);

            if (approveResult.Errors.Count > 0)
            {
                await AppServiceLocator.Alerts.DisplayAlert("Error", approveResult.Errors[0].Message, "OK");
                return;
            }

            await AppServiceLocator.Alerts.DisplayAlert("Success", "Your offer has been submitted to the Admin for final review.", "OK");
            await AppServiceLocator.Navigation.NavigateToAsync("/");
        }
        catch (Exception ex)
        {
            await AppServiceLocator.Alerts.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await AppServiceLocator.Navigation.NavigateToAsync("..");
    }
}




