using BuildSmart.Maui.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BuildSmart.Maui.ViewModels;

public partial class BidSubItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _comment = string.Empty;

    [ObservableProperty]
    private decimal? _price;

    [ObservableProperty]
    private int? _duration;

    [ObservableProperty]
    private string _durationUnit = "Days";

    [ObservableProperty]
    private bool _hasError;

    public IReadOnlyList<string> DurationUnits { get; } = new[] { "Hours", "Days", "Weeks", "Months" };
}

public partial class BidItemViewModel : ObservableObject
{
    public IGetJobTasks_AllJobPosts_JobTasks JobTask { get; }

    public ObservableCollection<BidSubItemViewModel> SubItems { get; } = new();

    public decimal TotalPrice => SubItems.Sum(x => x.Price ?? 0);

    public BidItemViewModel(IGetJobTasks_AllJobPosts_JobTasks jobTask)
    {
        JobTask = jobTask;
        var firstItem = new BidSubItemViewModel();
        firstItem.PropertyChanged += SubItem_PropertyChanged;
        SubItems.Add(firstItem);
    }

    [RelayCommand]
    private void AddSubItem()
    {
        var newItem = new BidSubItemViewModel();
        newItem.PropertyChanged += SubItem_PropertyChanged;
        SubItems.Add(newItem);
        OnPropertyChanged(nameof(TotalPrice));
    }

    [RelayCommand]
    private void RemoveSubItem(BidSubItemViewModel item)
    {
        if (SubItems.Count > 1)
        {
            item.PropertyChanged -= SubItem_PropertyChanged;
            SubItems.Remove(item);
            OnPropertyChanged(nameof(TotalPrice));
        }
    }

    private void SubItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BidSubItemViewModel.Price))
        {
            OnPropertyChanged(nameof(TotalPrice));
        }
    }
}

[QueryProperty(nameof(JobId), "jobId")]
public partial class PlaceBidViewModel : ObservableObject
{
    private readonly IBuildSmartApiClient _apiClient;

    public PlaceBidViewModel(IBuildSmartApiClient apiClient)
    {
        _apiClient = apiClient;
        EarliestStartDate = DateTime.Today.AddDays(1); // Default to tomorrow
    }

    [ObservableProperty]
    private string? _jobId;

    partial void OnJobIdChanged(string? value)
    {
        if (Guid.TryParse(value, out var id))
        {
            _ = LoadTasksAsync(id);
        }
    }

    public ObservableCollection<BidItemViewModel> BidItems { get; } = new();

    [ObservableProperty]
    private decimal _totalAmount;

    [ObservableProperty]
    private string _comment = string.Empty;

    [ObservableProperty]
    private DateTime _earliestStartDate;

    [ObservableProperty]
    private bool _isBusy;

    private async Task LoadTasksAsync(Guid jobId)
    {
        try
        {
            IsBusy = true;
            BidItems.Clear();
            var tasksResult = await _apiClient.GetJobTasks.ExecuteAsync(jobId);
            if (tasksResult.Errors.Count == 0 && tasksResult.Data?.AllJobPosts != null && tasksResult.Data.AllJobPosts.Count > 0)
            {
                foreach (var task in tasksResult.Data.AllJobPosts[0].JobTasks)
                {
                    var itemVm = new BidItemViewModel(task);
                    itemVm.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(BidItemViewModel.TotalPrice))
                        {
                            UpdateTotal();
                        }
                    };
                    BidItems.Add(itemVm);
                }
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateTotal()
    {
        TotalAmount = BidItems.Sum(x => x.TotalPrice);
    }

    [RelayCommand]
    private async Task SubmitBidAsync()
    {
        if (string.IsNullOrEmpty(JobId) || !Guid.TryParse(JobId, out var parsedJobId))
        {
            await Shell.Current.DisplayAlert("Error", "Invalid Job ID.", "OK");
            return;
        }

        if (BidItems.Count == 0)
        {
            await Shell.Current.DisplayAlert("Validation", "No tasks available to bid on.", "OK");
            return;
        }

        bool hasValidationErrors = false;
        foreach (var task in BidItems)
        {
            foreach (var subItem in task.SubItems)
            {
                if (subItem.Price == null || subItem.Price <= 0)
                {
                    subItem.HasError = true;
                    hasValidationErrors = true;
                }
                else
                {
                    subItem.HasError = false;
                }
            }
        }

        if (hasValidationErrors)
        {
            await Shell.Current.DisplayAlert("Validation", "Please enter a valid price for all tasks.", "OK");
            return;
        }

        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // Fetch current tradesman profile ID
            var userResult = await _apiClient.GetCurrentUser.ExecuteAsync();
            var tradesmanIdStr = userResult.Data?.CurrentUser?.TradesmanProfile?.Id;
            
            if (string.IsNullOrEmpty(tradesmanIdStr) || !Guid.TryParse(tradesmanIdStr, out var tradesmanId))
            {
                await Shell.Current.DisplayAlert("Error", "Could not verify your Tradesman Profile.", "OK");
                return;
            }

            var bidItemInputs = new List<BidItemInput>();
            foreach (var item in BidItems)
            {
                foreach (var sub in item.SubItems)
                {
                    string comment = sub.Comment;
                    if (sub.Duration.HasValue && sub.Duration.Value > 0)
                    {
                        string durationText = $"[Duration: {sub.Duration.Value} {sub.DurationUnit}]";
                        comment = string.IsNullOrWhiteSpace(comment) ? durationText : $"{durationText} {comment}";
                    }

                    bidItemInputs.Add(new BidItemInput
                    {
                        JobTaskId = item.JobTask.Id,
                        PriceSubtotal = sub.Price ?? 0,
                        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment
                    });
                }
            }

            var input = new SubmitBidInput
            {
                JobPostId = parsedJobId,
                TradesmanProfileId = tradesmanId,
                Currency = "USD",
                Comment = Comment,
                EarliestStartDate = EarliestStartDate,
                BidItems = bidItemInputs
            };

            var result = await _apiClient.SubmitBid.ExecuteAsync(input);

            if (result.Errors.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", result.Errors[0].Message, "OK");
                return;
            }

            await Shell.Current.DisplayAlert("Success", "Your bid has been placed successfully!", "OK");
            
            // Navigate back
            await Shell.Current.GoToAsync("..");
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

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
