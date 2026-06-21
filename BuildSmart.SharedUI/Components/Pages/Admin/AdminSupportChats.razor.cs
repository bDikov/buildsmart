using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuildSmart.SharedUI.GraphQL;

namespace BuildSmart.SharedUI.Components.Pages.Admin;

public partial class AdminSupportChats : ComponentBase, IAsyncDisposable
{
    private List<IGetActiveSupportChats_ActiveSupportChats> _activeChats = new();
    private bool _isLoading = true;
    private string _searchQuery = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadActiveChatsAsync();
        _isLoading = false;

        SignalRService.ProjectMessageReceived += OnProjectMessageReceived;
        await SignalRService.JoinSupportGroupAsync();
    }

    private async Task LoadActiveChatsAsync()
    {
        try
        {
            var result = await ApiClient.GetActiveSupportChats.ExecuteAsync();
            if (result.Errors.Count == 0 && result.Data?.ActiveSupportChats != null)
            {
                _activeChats = result.Data.ActiveSupportChats.ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading active support chats: {ex.Message}");
        }
    }

    private List<IGetActiveSupportChats_ActiveSupportChats> GetFilteredChats()
    {
        if (string.IsNullOrWhiteSpace(_searchQuery)) return _activeChats;

        return _activeChats.Where(c =>
            c.HomeownerName.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
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

    private void OpenChat(Guid projectId)
    {
        NavManager.NavigateTo($"/project-messages?projectId={projectId}");
    }

    private string FormatTime(DateTimeOffset date)
    {
        if (date.LocalDateTime.Date == DateTime.Today) return date.LocalDateTime.ToString("HH:mm");
        if (date.LocalDateTime.Date == DateTime.Today.AddDays(-1)) return "Вчера";
        return date.LocalDateTime.ToString("dd.MM.yyyy");
    }

    public async ValueTask DisposeAsync()
    {
        SignalRService.ProjectMessageReceived -= OnProjectMessageReceived;
        await SignalRService.LeaveSupportGroupAsync();
    }
}
