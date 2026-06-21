using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuildSmart.Core.Application.Services;

public class ProjectChatService : IProjectChatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IActiveProjectChatTracker _activeProjectChatTracker;

    public ProjectChatService(IUnitOfWork unitOfWork, INotificationService notificationService, IActiveProjectChatTracker activeProjectChatTracker)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _activeProjectChatTracker = activeProjectChatTracker;
    }

    public async Task<IEnumerable<ProjectMessage>> GetProjectMessagesAsync(Guid projectId, Guid userId, int offset, int limit)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
        if (project == null) throw new ArgumentException("Project not found");

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (project.HomeownerId != userId && user?.Role != Core.Domain.Enums.UserRoleTypes.Admin)
        {
            throw new UnauthorizedAccessException("Not authorized to view project messages.");
        }

        return await _unitOfWork.ProjectMessages.GetMessagesPaginatedAsync(projectId, offset, limit);
    }

    public async Task<ProjectMessage> SendMessageAsync(Guid projectId, Guid senderId, string messageText)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
        if (project == null) throw new ArgumentException("Project not found");

        var message = new ProjectMessage
        {
            ProjectId = projectId,
            SenderId = senderId,
            MessageText = messageText,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ProjectMessages.AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        var sender = await _unitOfWork.Users.GetByIdAsync(senderId);
        message.Sender = sender!;

        await _notificationService.NotifyProjectGroupAsync(projectId, "ReceiveProjectMessage", new
        {
            Id = message.Id,
            ProjectId = message.ProjectId,
            SenderId = message.SenderId,
            SenderName = $"{sender!.FirstName} {sender.LastName}",
            MessageText = message.MessageText,
            CreatedAt = message.CreatedAt
        });

        var snippet = messageText.Length > 60 ? messageText.Substring(0, 57) + "..." : messageText;

        // If the sender is not the homeowner, support replied
        if (senderId != project.HomeownerId)
        {
            // Only send the notification if the homeowner is not active on the chat page
            if (!_activeProjectChatTracker.IsUserActiveInProject(project.HomeownerId.ToString(), project.Id.ToString()))
            {
                await _notificationService.SendLocalizedNotificationAsync(
                    userId: project.HomeownerId,
                    titleKey: "Notification_SupportReply_Title",
                    messageKey: "Notification_SupportReply_Body",
                    messageArgs: new object[] { snippet },
                    relatedEntityId: project.Id,
                    relatedEntityType: "Project",
                    data: new { route = "ProjectMessages", projectId = project.Id.ToString() }
                );
            }
        }
        else
        {
            // Homeowner sent a message. Notify admins.
            var admins = await _unitOfWork.Users.GetQueryable()
                .Where(u => u.Role == UserRoleTypes.Admin)
                .ToListAsync();

            foreach (var admin in admins)
            {
                if (!_activeProjectChatTracker.IsUserActiveInProject(admin.Id.ToString(), project.Id.ToString()))
                {
                    await _notificationService.SendLocalizedNotificationAsync(
                        userId: admin.Id,
                        titleKey: "Notification_NewMessage_Title",
                        messageKey: "Notification_NewMessage_Body",
                        messageArgs: new object[] { snippet },
                        relatedEntityId: project.Id,
                        relatedEntityType: "Project",
                        data: new { route = "ProjectMessages", projectId = project.Id.ToString() }
                    );
                }
            }
        }

        return message;
    }
}
