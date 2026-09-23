using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BuildSmart.Core.Application.Services;

public class ProjectChatService : IProjectChatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IActiveProjectChatTracker _activeProjectChatTracker;
    private readonly ITelegramBotService? _telegramBotService;
    private readonly IAiService? _aiService;
    private readonly IRenovationEstimatorCalculator? _estimatorCalculator;
    private readonly ILogger<ProjectChatService>? _logger;
    private readonly ICalculatorLeadRepository? _calculatorLeadRepository;

    private static readonly ConcurrentDictionary<Guid, bool> _leadAlertedProjects = new();

    public ProjectChatService(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IActiveProjectChatTracker activeProjectChatTracker,
        ITelegramBotService? telegramBotService = null,
        IAiService? aiService = null,
        IRenovationEstimatorCalculator? estimatorCalculator = null,
        ILogger<ProjectChatService>? logger = null,
        ICalculatorLeadRepository? calculatorLeadRepository = null)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _activeProjectChatTracker = activeProjectChatTracker;
        _telegramBotService = telegramBotService;
        _aiService = aiService;
        _estimatorCalculator = estimatorCalculator;
        _logger = logger;
        _calculatorLeadRepository = calculatorLeadRepository;
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

        bool isClientMessage = sender?.Role != UserRoleTypes.Admin;
        string? detectedPhone = null;
        string? detectedEmail = null;

        if (isClientMessage)
        {
            try
            {
                var phones = BulgarianPhoneValidator.ExtractValidPhones(messageText, checkDummyPatterns: false);
                detectedPhone = phones.FirstOrDefault();

                var emails = BulgarianPhoneValidator.ExtractEmails(messageText);
                detectedEmail = emails.FirstOrDefault();

                bool userUpdated = false;

                if (!string.IsNullOrWhiteSpace(detectedPhone) && string.IsNullOrWhiteSpace(sender?.PhoneNumber))
                {
                    sender!.PhoneNumber = detectedPhone;
                    userUpdated = true;
                }

                if (!string.IsNullOrWhiteSpace(detectedEmail) && sender != null && sender.Email.EndsWith("@buildsmart.guest", StringComparison.OrdinalIgnoreCase))
                {
                    sender.Email = detectedEmail;
                    userUpdated = true;
                }

                // Check if user introduced their name: "казвам се Иван", "аз съм Петър Георгиев", "име: Димитър"
                var nameMatch = Regex.Match(messageText, @"(?:казвам се|аз съм|име(?:то ми е)?\s*[:\-]?)\s+([А-Яа-яA-Za-z]+(?:\s+[А-Яа-яA-Za-z]+)?)", RegexOptions.IgnoreCase);
                if (nameMatch.Success && sender != null && (sender.FirstName == "Guest" || string.IsNullOrWhiteSpace(sender.FirstName)))
                {
                    var fullName = nameMatch.Groups[1].Value.Trim();
                    var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        sender.FirstName = parts[0];
                        if (parts.Length > 1) sender.LastName = string.Join(" ", parts.Skip(1));
                        userUpdated = true;
                    }
                }

                if (userUpdated)
                {
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[ProjectChatService] Failed to extract contact info or enrich user profile for project {ProjectId}", projectId);
            }
        }

        // Send Telegram alert if the message is from homeowner/client
        if (isClientMessage && _telegramBotService != null)
        {
            try
            {
                var senderDisplayName = $"{sender?.FirstName} {sender?.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(senderDisplayName) || senderDisplayName == "Guest User") senderDisplayName = "Клиент";
                var effectivePhone = detectedPhone ?? sender?.PhoneNumber;
                var effectiveEmail = detectedEmail ?? (sender?.Email?.EndsWith("@buildsmart.guest", StringComparison.OrdinalIgnoreCase) == true ? null : sender?.Email);

                bool isNewContactProvided = !string.IsNullOrWhiteSpace(detectedPhone) || 
                    (!string.IsNullOrWhiteSpace(detectedEmail) && sender?.Email?.EndsWith("@buildsmart.guest", StringComparison.OrdinalIgnoreCase) == true);

                if (isNewContactProvided)
                {
                    _leadAlertedProjects[projectId] = true;

                    // Sync lead to CRM repository
                    if (_calculatorLeadRepository != null && !string.IsNullOrWhiteSpace(effectivePhone))
                    {
                        try
                        {
                            var lead = new CalculatorLead
                            {
                                Name = senderDisplayName,
                                Phone = effectivePhone,
                                Email = effectiveEmail ?? $"{projectId:N}@buildsmart.chat",
                                Scope = "ai_chat",
                                BuildingStatus = "unknown",
                                QualityTier = "standard",
                                AdminNotes = $"[AI_CHAT_LEAD]: {messageText}\nПроект: {project.Title}",
                                CreatedAt = DateTime.UtcNow
                            };
                            await _calculatorLeadRepository.AddLeadAsync(lead);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "[ProjectChatService] Failed to persist chat lead to CRM for project {ProjectId}", projectId);
                        }
                    }

                    // Send official Lead Alert to Telegram with WhatsApp quick action
                    await _telegramBotService.SendLeadAlertAsync(
                        projectId: projectId,
                        projectTitle: project.Title ?? "Запитване от AI Чат",
                        homeownerName: senderDisplayName,
                        homeownerPhone: effectivePhone,
                        homeownerEmail: effectiveEmail,
                        location: "София",
                        aiSummary: $"Съобщение от клиент:\n\"{messageText}\""
                    );
                }
                else
                {
                    await _telegramBotService.SendChatMessageAlertAsync(
                        projectId: projectId,
                        projectTitle: project.Title ?? "Чат по проект",
                        senderName: senderDisplayName,
                        messageText: messageText,
                        senderPhone: effectivePhone
                    );
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[ProjectChatService] Failed to dispatch Telegram alert for project {ProjectId}", projectId);
            }
        }

        // Always-On AI Assistant reply when homeowner/client sends a message
        // ONLY if a human admin is NOT currently active in this conversation
        if (isClientMessage && !IsHumanTakeoverActive(projectId))
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

                    var historySb = new StringBuilder();
                    var recentMessages = await _unitOfWork.ProjectMessages.GetMessagesPaginatedAsync(projectId, 0, 8);
                    if (recentMessages != null)
                    {
                        var chronological = recentMessages.Reverse().ToList();
                        foreach (var msg in chronological)
                        {
                            if (msg.Id == message.Id) continue;
                            var senderRole = msg.SenderId == project.HomeownerId ? "Клиент" : "BuildSmart Консултант";
                            historySb.AppendLine($"{senderRole}: \"{msg.MessageText}\"");
                        }
                    }

                    var contextSb = new StringBuilder();
                    contextSb.AppendLine($"Проект: {project.Title}");
                    if (!string.IsNullOrWhiteSpace(jobsSummary))
                    {
                        contextSb.AppendLine($"Дейности: {jobsSummary}");
                    }
                    if (historySb.Length > 0)
                    {
                        contextSb.AppendLine();
                        contextSb.AppendLine("История на текущия разговор до момента:");
                        contextSb.Append(historySb);
                    }

                    if (_estimatorCalculator != null && (_estimatorCalculator.IsPricingOrDimensionQuery(messageText) || !string.IsNullOrWhiteSpace(jobsSummary)))
                    {
                        contextSb.AppendLine();
                        contextSb.AppendLine(_estimatorCalculator.GetEstimatorKnowledgeSummary());
                    }

                    autoReplyText = await _aiService.GenerateChatReplyAsync(contextSb.ToString(), messageText, lang);
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
                        if (string.IsNullOrWhiteSpace(clientDisplayName) || clientDisplayName == "Guest User") clientDisplayName = "Клиент";
                        await _telegramBotService.SendAiReplyNotificationAsync(
                            projectId: projectId,
                            clientName: clientDisplayName,
                            aiReplyText: autoReplyText
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "[ProjectChatService] Failed to notify admin of AI reply for project {ProjectId}", projectId);
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
