using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuildSmart.SharedUI.GraphQL;

namespace BuildSmart.SharedUI.Components.Pages.Admin;

public partial class AdminSupportChats : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private List<ActiveChatState> _activeChats = new();
    private List<SearchResultState> _searchResults = new();
    private Dictionary<Guid, List<IGetAllProjects_AllProjects>> _homeownerProjects = new();
    private HashSet<Guid> _expandedHomeownerIds = new();
    
    private bool _isLoading = true;
    private string _searchQuery = string.Empty;
    private bool _isCreatingProject = false;

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery != value)
            {
                _searchQuery = value;
                OnSearchQueryChanged();
            }
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadActiveChatsAsync();
        _isLoading = false;

        SignalRService.ProjectMessageReceived += OnProjectMessageReceived;
        SignalRService.UserPresenceChanged += OnUserPresenceChanged;
        await SignalRService.JoinSupportGroupAsync();
    }

    private async Task LoadActiveChatsAsync()
    {
        try
        {
            var result = await ApiClient.GetActiveSupportChats.ExecuteAsync();
            if (result.Errors.Count == 0 && result.Data?.ActiveSupportChats != null)
            {
                _activeChats = result.Data.ActiveSupportChats.Select(c => new ActiveChatState
                {
                    ProjectId = c.ProjectId,
                    ProjectTitle = c.ProjectTitle,
                    HomeownerName = c.HomeownerName,
                    HomeownerEmail = c.HomeownerEmail,
                    HomeownerId = c.HomeownerId,
                    LatestMessageText = c.LatestMessageText,
                    LatestMessageTime = c.LatestMessageTime,
                    IsHomeownerOnline = c.IsHomeownerOnline
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading active support chats: {ex.Message}");
        }
    }

    private void OnSearchQueryChanged()
    {
        if (_searchQuery.Length >= 3)
        {
            _ = SearchUsersAsync(_searchQuery);
        }
        else
        {
            _searchResults.Clear();
            _expandedHomeownerIds.Clear();
            StateHasChanged();
        }
    }

    private async Task SearchUsersAsync(string query)
    {
        try
        {
            var filter = new UserFilterInput
            {
                Role = new UserRoleTypesOperationFilterInput
                {
                    Eq = UserRoleTypes.Homeowner
                },
                Or = new List<UserFilterInput>
                {
                    new UserFilterInput { Email = new StringOperationFilterInput { Contains = query } },
                    new UserFilterInput { FirstName = new StringOperationFilterInput { Contains = query } },
                    new UserFilterInput { LastName = new StringOperationFilterInput { Contains = query } }
                }
            };

            var result = await ApiClient.SearchUsers.ExecuteAsync(filter);
            if (result.Errors.Count == 0 && result.Data?.Users != null)
            {
                _searchResults = result.Data.Users.Select(u => new SearchResultState
                {
                    Id = Guid.Parse(u.Id),
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    IsOnline = u.IsOnline
                }).ToList();
            }
            else
            {
                _searchResults.Clear();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching users: {ex.Message}");
        }
        finally
        {
            StateHasChanged();
        }
    }

    private List<ActiveChatState> GetFilteredChats()
    {
        if (string.IsNullOrWhiteSpace(_searchQuery)) return _activeChats;

        return _activeChats.Where(c =>
            c.HomeownerName.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
            c.HomeownerEmail.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
            c.ProjectTitle.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
            c.LatestMessageText.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    private void OnProjectMessageReceived(System.Text.Json.JsonElement payload)
    {
        InvokeAsync(async () =>
        {
            await LoadActiveChatsAsync();
            StateHasChanged();
        });
    }

    private void OnUserPresenceChanged(string userId, bool isOnline)
    {
        InvokeAsync(() =>
        {
            var changed = false;

            // Update active chats
            foreach (var chat in _activeChats)
            {
                if (string.Equals(chat.HomeownerId.ToString(), userId, StringComparison.OrdinalIgnoreCase))
                {
                    if (chat.IsHomeownerOnline != isOnline)
                    {
                        chat.IsHomeownerOnline = isOnline;
                        changed = true;
                    }
                }
            }

            // Update search results
            foreach (var user in _searchResults)
            {
                if (string.Equals(user.Id.ToString(), userId, StringComparison.OrdinalIgnoreCase))
                {
                    if (user.IsOnline != isOnline)
                    {
                        user.IsOnline = isOnline;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                StateHasChanged();
            }
        });
    }

    private async Task OnHomeownerClickedAsync(Guid homeownerId, string firstName, string lastName)
    {
        if (_expandedHomeownerIds.Contains(homeownerId))
        {
            _expandedHomeownerIds.Remove(homeownerId);
            return;
        }

        _expandedHomeownerIds.Add(homeownerId);

        if (!_homeownerProjects.ContainsKey(homeownerId))
        {
            await LoadHomeownerProjectsAsync(homeownerId);
        }

        var projects = _homeownerProjects.GetValueOrDefault(homeownerId) ?? new();
        if (projects.Count == 1)
        {
            OpenChat(projects[0].Id);
        }
    }

    private async Task LoadHomeownerProjectsAsync(Guid homeownerId)
    {
        try
        {
            var filter = new ProjectFilterInput
            {
                HomeownerId = new UuidOperationFilterInput
                {
                    Eq = homeownerId
                }
            };

            var result = await ApiClient.GetAllProjects.ExecuteAsync(filter, null);
            if (result.Errors.Count == 0 && result.Data?.AllProjects != null)
            {
                _homeownerProjects[homeownerId] = result.Data.AllProjects.ToList();
            }
            else
            {
                _homeownerProjects[homeownerId] = new List<IGetAllProjects_AllProjects>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading homeowner projects: {ex.Message}");
            _homeownerProjects[homeownerId] = new List<IGetAllProjects_AllProjects>();
        }
    }

    private async Task CreateSupportProjectAndChatAsync(Guid homeownerId, string firstName, string lastName)
    {
        if (_isCreatingProject) return;
        _isCreatingProject = true;
        try
        {
            var title = $"Support - {firstName} {lastName}";
            var description = "Default support chat room.";
            var result = await ApiClient.CreateProject.ExecuteAsync(homeownerId, title, description, "en");
            if (result.Errors.Count == 0 && result.Data?.CreateProject != null)
            {
                var projectId = result.Data.CreateProject.Id;
                OpenChat(projectId);
            }
            else
            {
                Console.WriteLine($"Error creating support project: {string.Join(", ", result.Errors.Select(e => e.Message))}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception creating support project: {ex.Message}");
        }
        finally
        {
            _isCreatingProject = false;
        }
    }

    private void OpenChat(Guid projectId)
    {
        NavManager.NavigateTo($"/project-messages?projectId={projectId}");
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("chatHelpers.formatLocalTimes");
        }
        catch { }
    }

    private string FormatTime(DateTimeOffset date)
    {
        var local = date.Offset == TimeSpan.Zero ? date.ToLocalTime() : DateTime.SpecifyKind(date.DateTime, DateTimeKind.Utc).ToLocalTime();
        if (local.Date == DateTime.Today) return local.ToString("HH:mm");
        if (local.Date == DateTime.Today.AddDays(-1)) return "Yesterday";
        return local.ToString("dd.MM.yyyy");
    }

    public async ValueTask DisposeAsync()
    {
        SignalRService.ProjectMessageReceived -= OnProjectMessageReceived;
        SignalRService.UserPresenceChanged -= OnUserPresenceChanged;
        await SignalRService.LeaveSupportGroupAsync();
    }
}

public class ActiveChatState
{
    public Guid ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string HomeownerName { get; set; } = string.Empty;
    public string HomeownerEmail { get; set; } = string.Empty;
    public Guid HomeownerId { get; set; }
    public string LatestMessageText { get; set; } = string.Empty;
    public DateTimeOffset? LatestMessageTime { get; set; }
    public bool IsHomeownerOnline { get; set; }
}

public class SearchResultState
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
}
