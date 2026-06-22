using System;

namespace BuildSmart.Core.Application.Interfaces;

public interface IUserPresenceService
{
    void UserConnected(string connectionId, string userId);
    string? UserDisconnected(string connectionId);
    bool IsUserOnline(string userId);
}

