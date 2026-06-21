using BuildSmart.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BuildSmart.Core.Application.Interfaces;

public interface IProjectChatService
{
    Task<IEnumerable<ProjectMessage>> GetProjectMessagesAsync(Guid projectId, Guid userId, int offset, int limit);
    Task<ProjectMessage> SendMessageAsync(Guid projectId, Guid senderId, string messageText);
}
