using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Text.Json;
using BuildSmart.SharedUI.GraphQL;
using BuildSmart.SharedUI.Services;

namespace BuildSmart.SharedUI.Components.Pages;

public partial class ProjectMessages : ComponentBase, IAsyncDisposable
{
    [SupplyParameterFromQuery]
    public Guid? ProjectId { get; set; }

    private ElementReference _messageContainerRef;
    private ElementReference _textareaRef;
    private List<ChatMessageModel> _messages = new();
    private string _newMessageText = string.Empty;
    private string? _projectName;
    private Guid _currentUserId;
    private bool _isLoadingHistory = true;
    private bool _isLoadingMore = false;
    private bool _hasMoreHistory = true;
    private bool _shouldScrollToBottom = false;
    private bool _isGuest = false;

    private Guid? _activeLoadedProjectId;

    protected override void OnInitialized()
    {
        SignalRService.ProjectMessageReceived += OnMessageReceived;
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!ProjectId.HasValue)
        {
            await ResolveAndNavigateToActiveChatAsync();
            return;
        }

        if (_activeLoadedProjectId != ProjectId)
        {
            if (_activeLoadedProjectId.HasValue)
            {
                try
                {
                    await SignalRService.LeaveProjectGroupAsync(_activeLoadedProjectId.Value.ToString());
                }
                catch { }
            }

            _activeLoadedProjectId = ProjectId;
            _messages.Clear();
            _isLoadingHistory = true;
            _hasMoreHistory = true;
            _projectName = null;

            try
            {
                var token = await AuthService.GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);
                    var email = jwtToken.Claims.FirstOrDefault(c => 
                        c.Type == "email" || 
                        c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
                    _isGuest = email != null && email.EndsWith("@buildsmart.guest", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { }

            await LoadUserDataAndProjectAsync();
            await LoadHistoryAsync(0, 20);

            try
            {
                await ApiClient.MarkProjectNotificationsAsRead.ExecuteAsync(ProjectId.Value);
                SignalRService.NotifyNotificationsStateChanged();
            }
            catch { }

            try
            {
                await SignalRService.JoinProjectGroupAsync(ProjectId.Value.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProjectMessages] Failed to join SignalR group: {ex.Message}");
            }

            _isLoadingHistory = false;
            _shouldScrollToBottom = true;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("chatHelpers.initAutoResize", _textareaRef);
            }
            catch { }
        }

        try
        {
            await JSRuntime.InvokeVoidAsync("chatHelpers.formatLocalTimes");
        }
        catch { }

        if (_shouldScrollToBottom)
        {
            _shouldScrollToBottom = false;
            await ScrollToBottomAsync();
        }
    }

    private async Task LoadUserDataAndProjectAsync()
    {
        try
        {
            var userResult = await ApiClient.GetCurrentUser.ExecuteAsync();
            if (userResult.Data?.CurrentUser != null)
            {
                _currentUserId = Guid.Parse(userResult.Data.CurrentUser.Id);
                var email = userResult.Data.CurrentUser.Email;
                _isGuest = email != null && email.EndsWith("@buildsmart.guest", StringComparison.OrdinalIgnoreCase);
            }

            var projectResult = await ApiClient.GetProjectById.ExecuteAsync(ProjectId!.Value);
            if (projectResult.Data?.ProjectById != null)
            {
                _projectName = projectResult.Data.ProjectById.Title;
                if (_projectName == "Support Chat")
                {
                    _projectName = Loc["Nav_SupportChat"];
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading user data or project: {ex.Message}");
        }
    }

    private async Task LoadHistoryAsync(int offset, int limit)
    {
        try
        {
            var result = await ApiClient.GetProjectMessages.ExecuteAsync(ProjectId!.Value, offset, limit);
            if (result.Errors.Count > 0) return;

            if (result.Data?.ProjectMessages != null)
            {
                var newMessages = result.Data.ProjectMessages.Select(m => new ChatMessageModel
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}",
                    MessageText = m.MessageText,
                    CreatedAt = m.CreatedAt,
                    IsCurrentUser = m.SenderId == _currentUserId
                }).Reverse().ToList();

                if (newMessages.Count < limit)
                {
                    _hasMoreHistory = false;
                }

                if (offset == 0)
                {
                    _messages = newMessages;
                }
                else
                {
                    _messages.InsertRange(0, newMessages);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading messages history: {ex.Message}");
        }
    }

    private async Task OnScroll(EventArgs e)
    {
        if (_isLoadingMore || !_hasMoreHistory) return;

        try
        {
            var scrollTop = await JSRuntime.InvokeAsync<double>("chatHelpers.getScrollTop", _messageContainerRef);
            if (scrollTop <= 5) // Check if user scrolled near/to top
            {
                _isLoadingMore = true;
                StateHasChanged();

                // Save previous scroll height
                var prevHeight = await JSRuntime.InvokeAsync<double>("chatHelpers.getScrollHeight", _messageContainerRef);

                // Fetch older messages
                await LoadHistoryAsync(_messages.Count, 20);

                StateHasChanged();
                await Task.Delay(50); // Small render delay

                // Preserve scroll position
                var newHeight = await JSRuntime.InvokeAsync<double>("chatHelpers.getScrollHeight", _messageContainerRef);
                var diff = newHeight - prevHeight;
                await JSRuntime.InvokeVoidAsync("chatHelpers.setScrollTop", _messageContainerRef, diff);

                _isLoadingMore = false;
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during scroll processing: {ex.Message}");
        }
    }

    private void OnMessageReceived(JsonElement payload)
    {
        try
        {
            var message = MapSignalRMessage(payload);
            
            // Only append message if it belongs to current active project
            if (message.ProjectId == Guid.Empty || (ProjectId.HasValue && message.ProjectId == ProjectId.Value))
            {
                if (!_messages.Any(m => m.Id == message.Id))
                {
                    _messages.Add(message);
                    InvokeAsync(async () =>
                    {
                        StateHasChanged();
                        await ScrollToBottomAsync();
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing incoming SignalR message: {ex.Message}");
        }
    }

    private ChatMessageModel MapSignalRMessage(JsonElement payload)
    {
        var id = payload.GetProperty("id").GetGuid();
        var senderId = payload.GetProperty("senderId").GetGuid();
        var senderName = payload.GetProperty("senderName").GetString() ?? "Unknown";
        var messageText = payload.GetProperty("messageText").GetString() ?? string.Empty;
        var createdAt = payload.GetProperty("createdAt").GetDateTime();

        Guid messageProjectId = Guid.Empty;
        if (payload.TryGetProperty("projectId", out var pProp) || payload.TryGetProperty("ProjectId", out pProp))
        {
            if (pProp.ValueKind == JsonValueKind.String)
                Guid.TryParse(pProp.GetString(), out messageProjectId);
            else if (pProp.ValueKind != JsonValueKind.Null)
                Guid.TryParse(pProp.ToString(), out messageProjectId);
        }

        return new ChatMessageModel
        {
            Id = id,
            ProjectId = messageProjectId,
            SenderId = senderId,
            SenderName = senderName,
            MessageText = messageText,
            CreatedAt = createdAt,
            IsCurrentUser = senderId == _currentUserId
        };
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(_newMessageText)) return;

        var textToSend = _newMessageText.Trim();
        _newMessageText = string.Empty;

        try
        {
            await JSRuntime.InvokeVoidAsync("chatHelpers.resetHeight", _textareaRef);
        }
        catch { }

        try
        {
            var result = await ApiClient.SendProjectMessage.ExecuteAsync(ProjectId!.Value, textToSend);
            if (result.Errors.Count == 0 && result.Data?.SendProjectMessage != null)
            {
                var sentMsg = result.Data.SendProjectMessage;
                var optimisticMsg = new ChatMessageModel
                {
                    Id = sentMsg.Id,
                    SenderId = sentMsg.SenderId,
                    SenderName = _isGuest ? Loc["Chat_GuestName"] : Loc["Chat_Me"],
                    MessageText = sentMsg.MessageText,
                    CreatedAt = sentMsg.CreatedAt,
                    IsCurrentUser = true
                };

                // Add to messages if not already received via SignalR
                if (!_messages.Any(m => m.Id == optimisticMsg.Id))
                {
                    _messages.Add(optimisticMsg);
                    StateHasChanged();
                    await ScrollToBottomAsync();
                }
            }
            else if (result.Errors.Count > 0)
            {
                var errorMsg = result.Errors[0].Message;
                await AppServiceLocator.Alerts.DisplayAlert(
                    Loc["Chat_ErrorTitle"],
                    string.Format(Loc["Chat_SendErrorMessage"], errorMsg),
                    "OK"
                );
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
            await AppServiceLocator.Alerts.DisplayAlert(
                Loc["Chat_ErrorTitle"],
                string.Format(Loc["Chat_SendErrorMessage"], ex.Message),
                "OK"
            );
        }
    }

    private async Task OnInputKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await SendMessageAsync();
        }
    }

    private async Task ScrollToBottomAsync()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("chatHelpers.scrollToBottom", _messageContainerRef);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Scroll error: {ex.Message}");
        }
    }

    private void GoBack()
    {
        NavManager.NavigateTo($"/project-detail?projectId={ProjectId}");
    }

    private DateTime FormatToLocalDateTime(DateTimeOffset dt)
    {
        if (dt.Offset == TimeSpan.Zero)
        {
            return dt.ToLocalTime().DateTime;
        }

        var utcDate = DateTime.SpecifyKind(dt.DateTime, DateTimeKind.Utc);
        return utcDate.ToLocalTime();
    }

    private string FormatDateHeader(DateTime date)
    {
        if (date.Date == DateTime.Today) return "Днес";
        if (date.Date == DateTime.Today.AddDays(-1)) return "Вчера";
        return date.ToString("dd MMMM yyyy");
    }

    private MarkupString FormatMessageText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new MarkupString(string.Empty);

        var encoded = System.Net.WebUtility.HtmlEncode(text);
        var formatted = System.Text.RegularExpressions.Regex.Replace(
            encoded,
            @"\*\*(.*?)\*\*",
            "<strong>$1</strong>"
        );

        return new MarkupString(formatted);
    }

    public async ValueTask DisposeAsync()
    {
        SignalRService.ProjectMessageReceived -= OnMessageReceived;
        if (ProjectId.HasValue)
        {
            try
            {
                await SignalRService.LeaveProjectGroupAsync(ProjectId.Value.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProjectMessages] Failed to leave SignalR group: {ex.Message}");
            }
        }
    }

    private async Task ResolveAndNavigateToActiveChatAsync()
    {
        try
        {
            // 1. Try to find the target project ID from notifications
            try
            {
                var notesResult = await ApiClient.GetMyNotifications.ExecuteAsync();
                if (notesResult.Errors.Count == 0 && notesResult.Data?.MyNotifications != null)
                {
                    var chatNote = notesResult.Data.MyNotifications
                        .Where(n => n.RelatedEntityType == "Project" && n.RelatedEntityId.HasValue)
                        .OrderByDescending(n => !n.IsRead)
                        .ThenByDescending(n => n.CreatedAt)
                        .FirstOrDefault();

                    if (chatNote != null && chatNote.RelatedEntityId.HasValue)
                    {
                        NavManager.NavigateTo($"/project-messages?projectId={chatNote.RelatedEntityId.Value}", replace: true);
                        return;
                    }
                }
            }
            catch { }

            // 2. Check user's active projects
            var result = await ApiClient.GetMyProjects.ExecuteAsync();
            if (result.Errors.Count == 0 && result.Data?.MyProjects != null)
            {
                var actualProjects = result.Data.MyProjects.Where(p => p.Title != "Support Chat").ToList();
                if (actualProjects.Any())
                {
                    var supportChat = actualProjects.FirstOrDefault(p => p.Title.StartsWith("Support - "));
                    var targetId = supportChat?.Id ?? actualProjects.OrderByDescending(p => p.CreatedAt).First().Id;
                    NavManager.NavigateTo($"/project-messages?projectId={targetId}", replace: true);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProjectMessages] Error resolving active chat: {ex.Message}");
        }

        // 3. Fallback to /my-projects if no projects exist
        NavManager.NavigateTo("/my-projects", replace: true);
    }
}

public class ChatMessageModel
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public bool IsCurrentUser { get; set; }
}
