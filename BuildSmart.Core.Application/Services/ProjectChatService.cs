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
        if (!await IsUserAuthorizedForProjectAsync(project, user))
        {
                throw new UnauthorizedAccessException("Not authorized to view project messages.");
        }

        return await _unitOfWork.ProjectMessages.GetMessagesPaginatedAsync(projectId, offset, limit);
    }

    private async Task<bool> IsUserAuthorizedForProjectAsync(Project project, User? user)
    {
        if (user == null) return false;
        if (user.Role == UserRoleTypes.Admin) return true;
        if (project.HomeownerId == user.Id) return true;

        if (_unitOfWork.JobPosts != null)
        {
            var jobPosts = await _unitOfWork.JobPosts.GetJobsByProjectIdAsync(project.Id);
            if (jobPosts != null && jobPosts.Any(j => j.AssignedTradesmanId == user.Id))
            {
                return true;
            }

            if (user.Role == UserRoleTypes.Tradesman && _unitOfWork.TradesmanProfiles != null)
            {
                var tradesmanProfile = await _unitOfWork.TradesmanProfiles.GetByUserIdAsync(user.Id);
                if (tradesmanProfile != null && _unitOfWork.Bids != null)
                {
                    var bids = await _unitOfWork.Bids.GetBidsByTradesmanAsync(tradesmanProfile.Id);
                    var jobPostIds = jobPosts?.Select(j => j.Id).ToHashSet() ?? new HashSet<Guid>();
                    if (bids != null && bids.Any(b => jobPostIds.Contains(b.JobPostId)))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public async Task<ProjectMessage> SendMessageAsync(Guid projectId, Guid senderId, string messageText)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
        if (project == null) throw new ArgumentException("Project not found");

        var sender = await _unitOfWork.Users.GetByIdAsync(senderId);
        if (!await IsUserAuthorizedForProjectAsync(project, sender))
        {
            throw new UnauthorizedAccessException("Not authorized to send project messages.");
        }

        var message = new ProjectMessage
        {
            ProjectId = projectId,
            SenderId = senderId,
            MessageText = messageText,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ProjectMessages.AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

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

        // Auto-reply logic if this is the homeowner's first message
        if (senderId == project.HomeownerId)
        {
            var messages = await _unitOfWork.ProjectMessages.GetMessagesPaginatedAsync(projectId, 0, 2);
            var messageCount = messages.Count();
            if (messageCount == 1)
            {
                var adminUser = await _unitOfWork.Users.GetQueryable()
                    .FirstOrDefaultAsync(u => u.Role == UserRoleTypes.Admin);
                if (adminUser != null)
                {
                    var currentLang = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                    var isBg = currentLang.Equals("bg", StringComparison.OrdinalIgnoreCase);
                    var autoReplyText = isBg 
                        ? "Здравейте! Благодарим Ви за съобщението. Наш сътрудник ще се свърже с Вас възможно най-скоро."
                        : "Hello! Thank you for your message. A representative will get in touch with you as soon as possible.";
                    
                    var autoReply = new ProjectMessage
                    {
                        ProjectId = projectId,
                        SenderId = adminUser.Id,
                        MessageText = autoReplyText,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    await _unitOfWork.ProjectMessages.AddAsync(autoReply);
                    await _unitOfWork.SaveChangesAsync();
                    
                    await _notificationService.NotifyProjectGroupAsync(projectId, "ReceiveProjectMessage", new
                    {
                        Id = autoReply.Id,
                        ProjectId = autoReply.ProjectId,
                        SenderId = autoReply.SenderId,
                        SenderName = $"{adminUser.FirstName} {adminUser.LastName}",
                        MessageText = autoReply.MessageText,
                        CreatedAt = autoReply.CreatedAt
                    });
                }
            }
        }

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
