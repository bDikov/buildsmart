using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildSmart.Api.Workers;

public class LeadFollowUpReminderJob
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<LeadFollowUpReminderJob> _logger;

    public LeadFollowUpReminderJob(
        AppDbContext context,
        INotificationService notificationService,
        ILogger<LeadFollowUpReminderJob> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1)]
    [DisplayName("Check & Dispatch Due Lead Follow-Up Reminders")]
    public async Task RunCheckAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting scheduled lead follow-up reminder check...");

        var todayUtc = DateTime.UtcNow.Date;

        // Fetch leads that have a follow-up scheduled for today or earlier that haven't triggered a reminder yet
        var dueLeads = await _context.CalculatorLeads
            .Where(l => l.FollowUpDate.HasValue 
                     && l.FollowUpDate.Value.Date <= todayUtc 
                     && !l.FollowUpReminderSent)
            .ToListAsync(cancellationToken);

        if (!dueLeads.Any())
        {
            _logger.LogInformation("No pending lead follow-up reminders found.");
            return;
        }

        _logger.LogInformation("Found {Count} leads due for follow-up reminders.", dueLeads.Count);

        // Fetch all active admin users to receive the notification
        var adminUserIds = await _context.Users
            .Where(u => u.Role == UserRoleTypes.Admin)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (!adminUserIds.Any())
        {
            _logger.LogWarning("No admin users found to receive lead follow-up reminders.");
            return;
        }

        foreach (var lead in dueLeads)
        {
            var leadDisplayName = !string.IsNullOrWhiteSpace(lead.Name) ? lead.Name.Trim() : (lead.Phone ?? lead.Email);
            var leadContactInfo = !string.IsNullOrWhiteSpace(lead.Phone) ? lead.Phone.Trim() : lead.Email;

            foreach (var adminId in adminUserIds)
            {
                try
                {
                    await _notificationService.SendLocalizedNotificationAsync(
                        adminId,
                        "Title_LeadFollowUpReminder",
                        "Msg_LeadFollowUpReminder",
                        new object[] { leadDisplayName, leadContactInfo },
                        lead.Id,
                        "CalculatorLead");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send follow-up reminder notification for lead {LeadId} to admin {AdminId}.", lead.Id, adminId);
                }
            }

            lead.FollowUpReminderSent = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully processed {Count} lead follow-up reminders.", dueLeads.Count);
    }
}
