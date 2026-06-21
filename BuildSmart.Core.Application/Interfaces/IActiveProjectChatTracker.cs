using System;

namespace BuildSmart.Core.Application.Interfaces;

public interface IActiveProjectChatTracker
{
    void JoinProject(string connectionId, string userId, string projectId);
    void LeaveProject(string connectionId, string projectId);
    void RemoveConnection(string connectionId);
    bool IsUserActiveInProject(string userId, string projectId);
}
