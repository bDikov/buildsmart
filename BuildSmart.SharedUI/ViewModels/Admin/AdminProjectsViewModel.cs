using BuildSmart.SharedUI.GraphQL;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using BuildSmart.SharedUI.Services;

namespace BuildSmart.SharedUI.ViewModels.Admin;

public partial class AdminProjectsViewModel : ObservableObject, IDisposable
{
    private readonly IBuildSmartApiClient _apiClient;
    private readonly SignalRService _signalRService;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    public ObservableCollection<IGetAllProjects_AllProjects> Projects { get; } = new();

    public AdminProjectsViewModel(IBuildSmartApiClient apiClient, SignalRService signalRService)
    {
        _apiClient = apiClient;
        _signalRService = signalRService;
        
        _signalRService.OfferRegenerated += OnOfferRegenerated;
        _ = _signalRService.ConnectAsync();
    }

    private void OnOfferRegenerated(Guid projectId)
    {
        // Reload projects when a PDF finishes generating in the background
        _ = LoadProjectsAsync();
    }

    public void Dispose()
    {
        _signalRService.OfferRegenerated -= OnOfferRegenerated;
    }

    [RelayCommand]
    public async Task LoadProjectsAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            ProjectFilterInput? filter = null;
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var textFilter = new StringOperationFilterInput { Contains = SearchQuery };
                
                var orConditions = new List<ProjectFilterInput>
                {
                    new ProjectFilterInput { Title = textFilter },
                    new ProjectFilterInput { Description = textFilter },
                    new ProjectFilterInput { Homeowner = new UserFilterInput { Email = textFilter } },
                    new ProjectFilterInput { Homeowner = new UserFilterInput { FirstName = textFilter } },
                    new ProjectFilterInput { Homeowner = new UserFilterInput { LastName = textFilter } }
                };

                if (Guid.TryParse(SearchQuery, out var guid))
                {
                    orConditions.Add(new ProjectFilterInput { Id = new UuidOperationFilterInput { Eq = guid } });
                    orConditions.Add(new ProjectFilterInput { HomeownerId = new UuidOperationFilterInput { Eq = guid } });
                }

                filter = new ProjectFilterInput
                {
                    Or = orConditions
                };
            }

            var order = new List<ProjectSortInput>
            {
                new ProjectSortInput { CreatedAt = SortEnumType.Desc }
            };

            var result = await _apiClient.GetAllProjects.ExecuteAsync(filter, order);
            
            Projects.Clear();
            if (result.Errors.Count > 0)
            {
                throw new Exception($"GraphQL Error: {string.Join(", ", result.Errors.Select(e => e.Message))}");
            }
            else if (result.Data?.AllProjects != null)
            {
                foreach (var project in result.Data.AllProjects)
                {
                    Projects.Add(project);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }
}