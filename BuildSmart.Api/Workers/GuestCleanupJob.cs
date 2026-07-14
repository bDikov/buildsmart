using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using BuildSmart.Infrastructure.Persistence;
using Hangfire;
using System.ComponentModel;

namespace BuildSmart.Api.Workers;

public class GuestCleanupJob
{
    private readonly AppDbContext _context;
    private readonly ILogger<GuestCleanupJob> _logger;
    private static readonly TimeSpan ExpirationThreshold = TimeSpan.FromDays(7);

    public GuestCleanupJob(AppDbContext context, ILogger<GuestCleanupJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 0)]
    [DisplayName("Cleanup Expired Guest Sessions")]
    public async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        var cutoffTime = DateTime.UtcNow - ExpirationThreshold;
        _logger.LogInformation("Checking for expired guest users...");

        // 1. Identify expired guest user IDs
        var expiredGuestIds = await _context.Users
            .Where(u => u.Email.ToLower().EndsWith("@buildsmart.guest") 
                && u.CreatedAt < cutoffTime 
                && (u.LastSeenAt == null || u.LastSeenAt < cutoffTime))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (!expiredGuestIds.Any())
        {
            _logger.LogInformation("No expired guest users found.");
            return;
        }

        _logger.LogInformation("Found {Count} expired guest users to delete.", expiredGuestIds.Count);

        // 2. Delete projects belonging to these guests (handles Restrict constraints on Project.HomeownerId and ProjectMessage.SenderId)
        var projectsToDelete = await _context.Projects
            .Where(p => expiredGuestIds.Contains(p.HomeownerId))
            .ToListAsync(cancellationToken);

        if (projectsToDelete.Any())
        {
            _context.Projects.RemoveRange(projectsToDelete);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted {Count} projects associated with expired guest users.", projectsToDelete.Count);
        }

        // 3. Delete the guest users themselves
        var usersToDelete = await _context.Users
            .Where(u => expiredGuestIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        if (usersToDelete.Any())
        {
            _context.Users.RemoveRange(usersToDelete);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully deleted {Count} expired guest users.", usersToDelete.Count);
        }
    }
}
