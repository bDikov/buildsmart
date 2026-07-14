using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildSmart.Api.Workers;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BuildSmart.Api.Tests.Workers;

public class GuestCleanupJobTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;

    public GuestCleanupJobTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task RunCleanupAsync_ShouldDeleteExpiredGuestsAndProjects_ButKeepActiveGuestsAndStandardUsers()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var expiredThreshold = now.AddDays(-8);
        var activeThreshold = now.AddDays(-1);

        using var context = new AppDbContext(_dbOptions);

        // 1. Expired guest: Created 8 days ago, LastSeen 8 days ago
        var expiredGuest = new User
        {
            Id = Guid.NewGuid(),
            Email = "guest_expired@buildsmart.guest",
            FirstName = "Expired",
            LastName = "Guest",
            CreatedAt = expiredThreshold,
            LastSeenAt = expiredThreshold
        };

        // 2. Active guest: Created 8 days ago, LastSeen 1 day ago
        var activeGuest = new User
        {
            Id = Guid.NewGuid(),
            Email = "guest_active@buildsmart.guest",
            FirstName = "Active",
            LastName = "Guest",
            CreatedAt = expiredThreshold,
            LastSeenAt = activeThreshold
        };

        // 3. New guest: Created 2 days ago, LastSeen null
        var newGuest = new User
        {
            Id = Guid.NewGuid(),
            Email = "guest_new@buildsmart.guest",
            FirstName = "New",
            LastName = "Guest",
            CreatedAt = now.AddDays(-2),
            LastSeenAt = null
        };

        // 4. Standard user: Created 30 days ago, LastSeen 30 days ago
        var standardUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "standard@example.com",
            FirstName = "Standard",
            LastName = "User",
            CreatedAt = now.AddDays(-30),
            LastSeenAt = now.AddDays(-30),
            HashedPassword = "hashed_password"
        };

        context.Users.AddRange(expiredGuest, activeGuest, newGuest, standardUser);

        // Support projects
        var expiredGuestProject = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Support Chat",
            Description = "Expired support chat",
            HomeownerId = expiredGuest.Id
        };

        var activeGuestProject = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Support Chat",
            Description = "Active support chat",
            HomeownerId = activeGuest.Id
        };

        context.Projects.AddRange(expiredGuestProject, activeGuestProject);
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<GuestCleanupJob>>();
        var job = new GuestCleanupJob(context, loggerMock.Object);

        // Act
        await job.RunCleanupAsync(CancellationToken.None);

        // Assert
        context.ChangeTracker.Clear();

        // Expired guest should be deleted
        var deletedGuest = await context.Users.FindAsync(expiredGuest.Id);
        deletedGuest.Should().BeNull();

        var deletedProject = await context.Projects.FindAsync(expiredGuestProject.Id);
        deletedProject.Should().BeNull();

        // Active guest should be kept
        var keptActiveGuest = await context.Users.FindAsync(activeGuest.Id);
        keptActiveGuest.Should().NotBeNull();

        var keptActiveProject = await context.Projects.FindAsync(activeGuestProject.Id);
        keptActiveProject.Should().NotBeNull();

        // New guest should be kept
        var keptNewGuest = await context.Users.FindAsync(newGuest.Id);
        keptNewGuest.Should().NotBeNull();

        // Standard user should be kept
        var keptStandardUser = await context.Users.FindAsync(standardUser.Id);
        keptStandardUser.Should().NotBeNull();
    }
}
