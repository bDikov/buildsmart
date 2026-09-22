using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildSmart.Api.Workers;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BuildSmart.Api.Tests.Services;

public class CalculatorLeadCrmTests
{
    private DbContextOptions<AppDbContext> CreateNewContextOptions()
    {
        // Rule 5: Total database isolation with unique InMemory database names
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"CalculatorLeadCrmDb_{Guid.NewGuid()}")
            .Options;
    }

    [Fact]
    public async Task PersistLead_ShouldStoreAndRetrieveCrmFieldsCorrectly()
    {
        // Arrange
        var options = CreateNewContextOptions();
        var leadId = Guid.NewGuid();
        var meetingTime = DateTime.UtcNow.AddDays(2);
        var followUpDay = DateTime.UtcNow.Date.AddDays(1);

        // Act - Seed in first DbContext
        await using (var seedContext = new AppDbContext(options))
        {
            var lead = new CalculatorLead
            {
                Id = leadId,
                Email = "prospect@buildsmart.bg",
                Phone = "+359888999888",
                Name = "Иван Димитров",
                Scope = "full",
                SelectedArea = 120,
                BuildingStatus = "bds",
                QualityTier = "premium",
                CallOutcome = "WillingToWork",
                ProspectRating = 9,
                MeetingStatus = "Scheduled",
                MeetingDate = meetingTime,
                MeetingNotes = "Оглед на място за двустаен апартамент в кв. Лозенец",
                ReadyToStartTimeline = "1_month",
                FollowUpDate = followUpDay,
                FollowUpReminderSent = false
            };

            await seedContext.CalculatorLeads.AddAsync(lead);
            await seedContext.SaveChangesAsync();
        }

        // Assert - Verify in separate DbContext (cleared EF tracking cache)
        await using (var verifyContext = new AppDbContext(options))
        {
            var retrieved = await verifyContext.CalculatorLeads.FindAsync(leadId);

            retrieved.Should().NotBeNull();
            retrieved!.CallOutcome.Should().Be("WillingToWork");
            retrieved.ProspectRating.Should().Be(9);
            retrieved.MeetingStatus.Should().Be("Scheduled");
            retrieved.MeetingDate.Should().BeCloseTo(meetingTime, TimeSpan.FromSeconds(1));
            retrieved.MeetingNotes.Should().Be("Оглед на място за двустаен апартамент в кв. Лозенец");
            retrieved.ReadyToStartTimeline.Should().Be("1_month");
            retrieved.FollowUpDate.Should().Be(followUpDay);
            retrieved.FollowUpReminderSent.Should().BeFalse();
        }
    }

    [Fact]
    public async Task QueryLeads_ShouldFilterByProspectRatingAndMeetingStatus()
    {
        // Arrange
        var options = CreateNewContextOptions();

        await using (var seedContext = new AppDbContext(options))
        {
            var leads = new List<CalculatorLead>
            {
                new()
                {
                    Email = "lead1@test.bg",
                    Phone = "+359888111111",
                    ProspectRating = 9,
                    CallOutcome = "WillingToWork",
                    MeetingStatus = "Scheduled"
                },
                new()
                {
                    Email = "lead2@test.bg",
                    Phone = "+359888222222",
                    ProspectRating = 6,
                    CallOutcome = "Possible",
                    MeetingStatus = "Requested"
                },
                new()
                {
                    Email = "lead3@test.bg",
                    Phone = "+359888333333",
                    ProspectRating = 2,
                    CallOutcome = "Pointless",
                    MeetingStatus = "Cancelled"
                },
                new()
                {
                    Email = "lead4@test.bg",
                    Phone = "+359888444444",
                    ProspectRating = 10,
                    CallOutcome = "WillingToWork",
                    MeetingStatus = "Completed"
                }
            };

            await seedContext.CalculatorLeads.AddRangeAsync(leads);
            await seedContext.SaveChangesAsync();
        }

        // Act & Assert in separate context
        await using (var queryContext = new AppDbContext(options))
        {
            var highProspects = await queryContext.CalculatorLeads
                .Where(l => l.ProspectRating >= 8)
                .ToListAsync();

            highProspects.Should().HaveCount(2);
            highProspects.Select(l => l.Email).Should().Contain(new[] { "lead1@test.bg", "lead4@test.bg" });

            var scheduledMeetings = await queryContext.CalculatorLeads
                .Where(l => l.MeetingStatus == "Scheduled")
                .ToListAsync();

            scheduledMeetings.Should().HaveCount(1);
            scheduledMeetings.First().Email.Should().Be("lead1@test.bg");

            var requestedMeetings = await queryContext.CalculatorLeads
                .Where(l => l.MeetingStatus == "Requested")
                .ToListAsync();

            requestedMeetings.Should().HaveCount(1);
            requestedMeetings.First().Email.Should().Be("lead2@test.bg");
        }
    }

    [Fact]
    public async Task LeadFollowUpReminderJob_ShouldDispatchNotifications_ForDueLeadsAndMarkAsSent()
    {
        // Arrange
        var options = CreateNewContextOptions();
        var adminId = Guid.NewGuid();
        var overdueLeadId = Guid.NewGuid();
        var todayLeadId = Guid.NewGuid();
        var futureLeadId = Guid.NewGuid();
        var alreadySentLeadId = Guid.NewGuid();

        await using (var seedContext = new AppDbContext(options))
        {
            // Seed Admin user
            await seedContext.Users.AddAsync(new User
            {
                Id = adminId,
                Email = "admin@buildsmart.bg",
                FirstName = "Admin",
                LastName = "User",
                Role = UserRoleTypes.Admin
            });

            // Seed Leads with various follow-up states
            await seedContext.CalculatorLeads.AddRangeAsync(
                // 1. Overdue lead (yesterday) -> should trigger
                new CalculatorLead
                {
                    Id = overdueLeadId,
                    Email = "overdue@test.bg",
                    Phone = "+359888123123",
                    Name = "Петър Иванов",
                    FollowUpDate = DateTime.UtcNow.Date.AddDays(-1),
                    FollowUpReminderSent = false
                },
                // 2. Due today -> should trigger
                new CalculatorLead
                {
                    Id = todayLeadId,
                    Email = "today@test.bg",
                    Phone = "+359888456456",
                    Name = "Георги Георгиев",
                    FollowUpDate = DateTime.UtcNow.Date,
                    FollowUpReminderSent = false
                },
                // 3. Future lead (due in 3 days) -> should NOT trigger
                new CalculatorLead
                {
                    Id = futureLeadId,
                    Email = "future@test.bg",
                    Phone = "+359888789789",
                    Name = "Мария Тодорова",
                    FollowUpDate = DateTime.UtcNow.Date.AddDays(3),
                    FollowUpReminderSent = false
                },
                // 4. Already sent -> should NOT trigger again
                new CalculatorLead
                {
                    Id = alreadySentLeadId,
                    Email = "alreadysent@test.bg",
                    Phone = "+359888999999",
                    Name = "Стоян Стоянов",
                    FollowUpDate = DateTime.UtcNow.Date.AddDays(-2),
                    FollowUpReminderSent = true
                }
            );

            await seedContext.SaveChangesAsync();
        }

        var mockNotificationService = new Mock<INotificationService>();
        var mockLogger = new Mock<ILogger<LeadFollowUpReminderJob>>();

        // Act - Run the reminder job using an active context
        await using (var jobContext = new AppDbContext(options))
        {
            var job = new LeadFollowUpReminderJob(jobContext, mockNotificationService.Object, mockLogger.Object);
            await job.RunCheckAsync(CancellationToken.None);
        }

        // Assert - Verify notifications dispatched to admin (all 7 arguments explicitly provided)
        mockNotificationService.Verify(
            s => s.SendLocalizedNotificationAsync(
                adminId,
                "Title_LeadFollowUpReminder",
                "Msg_LeadFollowUpReminder",
                It.Is<object[]>(args => args.Length == 2),
                It.Is<Guid?>(id => id == overdueLeadId || id == todayLeadId),
                "CalculatorLead",
                null),
            Times.Exactly(2));

        // Assert - Verify database state in a fresh context
        await using (var verifyContext = new AppDbContext(options))
        {
            var overdueLead = await verifyContext.CalculatorLeads.FindAsync(overdueLeadId);
            var todayLead = await verifyContext.CalculatorLeads.FindAsync(todayLeadId);
            var futureLead = await verifyContext.CalculatorLeads.FindAsync(futureLeadId);
            var alreadySentLead = await verifyContext.CalculatorLeads.FindAsync(alreadySentLeadId);

            overdueLead!.FollowUpReminderSent.Should().BeTrue();
            todayLead!.FollowUpReminderSent.Should().BeTrue();
            futureLead!.FollowUpReminderSent.Should().BeFalse();
            alreadySentLead!.FollowUpReminderSent.Should().BeTrue();
        }
    }

    [Fact]
    public async Task LeadFollowUpReminderJob_WhenNoAdminsExist_ShouldNotFailAndNotMarkSent()
    {
        // Arrange
        var options = CreateNewContextOptions();
        var leadId = Guid.NewGuid();

        await using (var seedContext = new AppDbContext(options))
        {
            // Seed due lead but NO admins
            await seedContext.CalculatorLeads.AddAsync(new CalculatorLead
            {
                Id = leadId,
                Email = "due@test.bg",
                Phone = "+359888555666",
                FollowUpDate = DateTime.UtcNow.Date,
                FollowUpReminderSent = false
            });
            await seedContext.SaveChangesAsync();
        }

        var mockNotificationService = new Mock<INotificationService>();
        var mockLogger = new Mock<ILogger<LeadFollowUpReminderJob>>();

        // Act
        await using (var jobContext = new AppDbContext(options))
        {
            var job = new LeadFollowUpReminderJob(jobContext, mockNotificationService.Object, mockLogger.Object);
            await job.RunCheckAsync(CancellationToken.None);
        }

        // Assert
        mockNotificationService.Verify(
            s => s.SendLocalizedNotificationAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<object?>()),
            Times.Never);

        await using (var verifyContext = new AppDbContext(options))
        {
            var lead = await verifyContext.CalculatorLeads.FindAsync(leadId);
            lead!.FollowUpReminderSent.Should().BeFalse();
        }
    }
}
