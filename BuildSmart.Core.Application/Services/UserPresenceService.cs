using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Enums;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace BuildSmart.Core.Application.Services;

public class UserPresenceService : IUserPresenceService
{
    private readonly ConcurrentDictionary<string, string> _activeConnections = new();

    public void UserConnected(string connectionId, string userId)
    {
        _activeConnections[connectionId] = userId;
    }

    public string? UserDisconnected(string connectionId)
    {
        if (_activeConnections.TryRemove(connectionId, out var userId))
        {
            return userId;
        }
        return null;
    }

    public bool IsUserOnline(string userId)
    {
        return _activeConnections.Values.Any(u => string.Equals(u, userId, StringComparison.OrdinalIgnoreCase));
    }

    public UserActiveStatus GetUserActiveStatus(string userId, DateTime? lastSeenAt)
    {
        if (IsUserOnline(userId))
        {
            return UserActiveStatus.Online;
        }

        if (lastSeenAt.HasValue)
        {
            if (DateTime.UtcNow - lastSeenAt.Value <= TimeSpan.FromHours(24))
            {
                return UserActiveStatus.RecentlyOnline;
            }
        }

        return UserActiveStatus.Offline;
    }
}
