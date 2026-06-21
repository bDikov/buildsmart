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
    private List<ChatMessageModel> _messages = new();
    private string _newMessageText = string.Empty;
    private string? _projectName;
    private Guid _currentUserId;
    private bool _isLoadingHistory = true;
    private bool _isLoadingMore = false;
    private bool _hasMoreHistory = true;

    protected override async Task OnInitializedAsync()
    {
        if (!ProjectId.HasValue)
        {
            NavManager.NavigateTo("/my-projects");
            return;
        }

        await LoadUserDataAndProjectAsync();
        await LoadHistoryAsync(0, 20);

        try
        {
            await ApiClient.MarkProjectNotificationsAsRead.ExecuteAsync(ProjectId.Value);
            SignalRService.NotifyNotificationsStateChanged();
        }
        catch { }

        // SignalR connection
        SignalRService.ProjectMessageReceived += OnMessageReceived;
        await SignalRService.JoinProjectGroupAsync(ProjectId.Value.ToString());

        _isLoadingHistory = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
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
            }

            var projectResult = await ApiClient.GetProjectById.ExecuteAsync(ProjectId!.Value);
            if (projectResult.Data?.ProjectById != null)
            {
                _projectName = projectResult.Data.ProjectById.Title;
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
            
            // Check if message is already in list
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

        return new ChatMessageModel
        {
            Id = id,
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
            var result = await ApiClient.SendProjectMessage.ExecuteAsync(ProjectId!.Value, textToSend);
            if (result.Errors.Count == 0 && result.Data?.SendProjectMessage != null)
            {
                // The message will be broadcast to the group via SignalR,
                // which will trigger OnMessageReceived and add it.
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
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

    private string FormatDate(DateTime date)
    {
        if (date.Date == DateTime.Today) return "Днес";
        if (date.Date == DateTime.Today.AddDays(-1)) return "Вчера";
        return date.ToString("dd MMMM yyyy");
    }

    public async ValueTask DisposeAsync()
    {
        SignalRService.ProjectMessageReceived -= OnMessageReceived;
        if (ProjectId.HasValue)
        {
            await SignalRService.LeaveProjectGroupAsync(ProjectId.Value.ToString());
        }
    }
}

public class ChatMessageModel
{
    public Guid Id { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public bool IsCurrentUser { get; set; }
}
