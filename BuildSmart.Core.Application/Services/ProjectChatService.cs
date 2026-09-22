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
    private readonly ITelegramBotService? _telegramBotService;
    private readonly IAiService? _aiService;

    public ProjectChatService(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IActiveProjectChatTracker activeProjectChatTracker,
        ITelegramBotService? telegramBotService = null,
        IAiService? aiService = null)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _activeProjectChatTracker = activeProjectChatTracker;
        _telegramBotService = telegramBotService;
        _aiService = aiService;
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

            if (await _unitOfWork.JobPosts.IsTradesmanAssignedToCategoryAsync(project.Id, user.Id))
            {
                return true;
            }
        }

        if (_unitOfWork.Bookings != null)
        {
            var isBooked = await _unitOfWork.Bookings.GetQueryable()
                .AnyAsync(b => b.JobPost.ProjectId == project.Id && b.TradesmanProfile.UserId == user.Id);
            if (isBooked) return true;
        }

        return false;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, DateTime> _humanTakeoverExpiry = new();

    public static void SetHumanTakeover(Guid projectId, bool active, TimeSpan? duration = null)
    {
        if (active)
        {
            _humanTakeoverExpiry[projectId] = DateTime.UtcNow.Add(duration ?? TimeSpan.FromMinutes(30));
        }
        else
        {
            _humanTakeoverExpiry.TryRemove(projectId, out _);
        }
    }

    public static bool IsHumanTakeoverActive(Guid projectId)
    {
        return _humanTakeoverExpiry.TryGetValue(projectId, out var expiry) && expiry > DateTime.UtcNow;
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

        // If a real human admin sends a message, activate Human Takeover (suppress AI for 30 minutes)
        if (sender?.Role == UserRoleTypes.Admin)
        {
            SetHumanTakeover(projectId, true, TimeSpan.FromMinutes(30));
        }

        // Send Telegram alert if the message is from homeowner/client
        if (senderId == project.HomeownerId && _telegramBotService != null)
        {
            try
            {
                var senderDisplayName = $"{sender?.FirstName} {sender?.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(senderDisplayName)) senderDisplayName = "Клиент";
                await _telegramBotService.SendChatMessageAlertAsync(
                    projectId: projectId,
                    projectTitle: project.Title ?? "Чат по проект",
                    senderName: senderDisplayName,
                    messageText: messageText,
                    senderPhone: sender?.PhoneNumber
                );
            }
            catch
            {
                // Never disrupt chat flow if Telegram encounters network error
            }
        }

        // Always-On AI Assistant reply when homeowner/client sends a message
        // ONLY if a human admin is NOT currently active in this conversation
        if (senderId == project.HomeownerId && !IsHumanTakeoverActive(projectId))
        {
            var adminUser = await _unitOfWork.Users.GetQueryable()
                .FirstOrDefaultAsync(u => u.Role == UserRoleTypes.Admin);
            if (adminUser != null)
            {
                var lang = project.LanguageCode ?? "bg";
                var isBg = lang.Equals("bg", StringComparison.OrdinalIgnoreCase);

                string autoReplyText;
                if (_aiService != null)
                {
                    var jobPosts = _unitOfWork.JobPosts != null 
                        ? await _unitOfWork.JobPosts.GetJobsByProjectIdAsync(projectId) 
                        : null;
                    var jobsSummary = jobPosts != null && jobPosts.Any()
                        ? string.Join("; ", jobPosts.Select(j => $"{j.Title} ({j.ServiceCategory?.Name ?? "Обща"}): {j.Description}"))
                        : project.Description;

                    var context = $"Проект: {project.Title}. Дейности: {jobsSummary}.";
                    autoReplyText = await _aiService.GenerateChatReplyAsync(context, messageText, lang);
                }
                else
                {
                    autoReplyText = isBg 
                        ? "Здравейте! Благодарим Ви за съобщението. Наш сътрудник ще се свърже с Вас възможно най-скоро."
                        : "Hello! Thank you for your message. A representative will get in touch with you as soon as possible.";
                }
                
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
                    SenderName = isBg ? "BuildSmart AI Консултант" : "BuildSmart AI Assistant",
                    MessageText = autoReply.MessageText,
                    CreatedAt = autoReply.CreatedAt
                });

                // Also notify admin in Telegram what the AI answered
                if (_telegramBotService != null)
                {
                    try
                    {
                        var clientDisplayName = $"{sender?.FirstName} {sender?.LastName}".Trim();
                        if (string.IsNullOrWhiteSpace(clientDisplayName)) clientDisplayName = "Клиент";
                        await _telegramBotService.SendAiReplyNotificationAsync(
                            projectId: projectId,
                            clientName: clientDisplayName,
                            aiReplyText: autoReplyText
                        );
                    }
                    catch
                    {
                        // Ignore Telegram alert errors
                    }
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
