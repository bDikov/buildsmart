using BuildSmart.Core.Application.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace BuildSmart.Core.Application.Services;

public class ActiveProjectChatTracker : IActiveProjectChatTracker
{
    private readonly ConcurrentDictionary<string, (string UserId, string ProjectId)> _activeConnections = new();

    public void JoinProject(string connectionId, string userId, string projectId)
    {
        _activeConnections[connectionId] = (userId, projectId);
    }

    public void LeaveProject(string connectionId, string projectId)
    {
        _activeConnections.TryRemove(connectionId, out _);
    }

    public void RemoveConnection(string connectionId)
    {
        _activeConnections.TryRemove(connectionId, out _);
    }

    public bool IsUserActiveInProject(string userId, string projectId)
    {
        return _activeConnections.Values.Any(c => 
            string.Equals(c.UserId, userId, StringComparison.OrdinalIgnoreCase) && 
            string.Equals(c.ProjectId, projectId, StringComparison.OrdinalIgnoreCase));
    }
}
