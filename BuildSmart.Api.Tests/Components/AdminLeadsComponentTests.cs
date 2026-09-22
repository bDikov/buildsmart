using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Bunit;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.SharedUI.Components.Pages.Admin;
using BuildSmart.SharedUI.Resources;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace BuildSmart.Api.Tests.Components;

public class AdminLeadsComponentTests : TestContext
{
    private readonly Mock<ICalculatorLeadRepository> _leadRepoMock = new();
    private readonly Mock<IStringLocalizer<AppResources>> _locMock = new();

    public AdminLeadsComponentTests()
    {
        // 1. Setup localizer to return the key
        _locMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        Services.AddSingleton(_locMock.Object);

        // 2. Setup mock repository
        Services.AddSingleton(_leadRepoMock.Object);

        // 3. Setup standard Blazor Authorization with Admin role
        Services.AddAuthorizationCore();
        Services.AddScoped<AuthenticationStateProvider, TestAdminAuthStateProvider>();

        // 4. Setup JSInterop
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void AdminLeads_WhenRendered_ShouldDisplayCrmRemindersBannerAndAccurateCounts()
    {
        // Arrange
        var testLeads = new List<CalculatorLead>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Overdue Client",
                Email = "overdue@test.bg",
                FollowUpDate = DateTime.UtcNow.Date.AddDays(-1),
                FollowUpReminderSent = false,
                ProspectRating = 6
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Today Client",
                Email = "today@test.bg",
                FollowUpDate = DateTime.UtcNow.Date,
                FollowUpReminderSent = false,
                ProspectRating = 7
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Meeting Client",
                Email = "meeting@test.bg",
                MeetingStatus = "Scheduled",
                MeetingDate = DateTime.UtcNow.AddDays(2),
                ProspectRating = 8,
                CallOutcome = "WillingToWork"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "High Prospect Client",
                Email = "high@test.bg",
                ProspectRating = 10,
                CallOutcome = "WillingToWork"
            }
        };

        _leadRepoMock.Setup(r => r.GetLeadsAsync()).ReturnsAsync(testLeads);

        // Act
        var cut = Render<AdminLeads>();

        // Assert - CRM Banner rendered
        var banner = cut.Find(".crm-reminders-banner");
        banner.Should().NotBeNull();

        // Assert - Overdue count
        var overdueVal = cut.Find(".reminder-overdue .reminder-count").TextContent.Trim();
        overdueVal.Should().Be("1");

        // Assert - Today count
        var todayVal = cut.Find(".reminder-today .reminder-count").TextContent.Trim();
        todayVal.Should().Be("1");

        // Assert - Meeting count
        var meetingVal = cut.Find(".reminder-meeting .reminder-count").TextContent.Trim();
        meetingVal.Should().Be("1");

        // Assert - High prospects count (ratings 8 and 10 -> 2)
        var prospectVal = cut.Find(".reminder-prospect .reminder-count").TextContent.Trim();
        prospectVal.Should().Be("2");
    }

    [Fact]
    public void AdminLeads_ClickingReminderCard_ShouldFilterLeadsTable()
    {
        // Arrange
        var overdueId = Guid.NewGuid();
        var normalId = Guid.NewGuid();
        var testLeads = new List<CalculatorLead>
        {
            new()
            {
                Id = overdueId,
                Name = "Overdue Client",
                Email = "overdue@test.bg",
                FollowUpDate = DateTime.UtcNow.Date.AddDays(-2),
                FollowUpReminderSent = false
            },
            new()
            {
                Id = normalId,
                Name = "Normal Client",
                Email = "normal@test.bg",
                FollowUpDate = DateTime.UtcNow.Date.AddDays(5),
                FollowUpReminderSent = false
            }
        };

        _leadRepoMock.Setup(r => r.GetLeadsAsync()).ReturnsAsync(testLeads);

        // Act
        var cut = Render<AdminLeads>();

        // Initially both leads shown
        cut.FindAll("tbody tr").Count.Should().Be(2);

        // Click the overdue reminder card
        var overdueCard = cut.Find(".reminder-overdue");
        overdueCard.Click();

        // Assert - Only 1 row matching overdue filter remains
        var rows = cut.FindAll("tbody tr");
        rows.Count.Should().Be(1);
        rows[0].TextContent.Should().Contain("Overdue Client");
    }

    [Fact]
    public void AdminLeads_WhenClientIsUncontacted_CrmAssessmentMenuShouldBeHidden_UntilMarkedContacted()
    {
        // Arrange
        var leadId = Guid.NewGuid();
        var targetLead = new CalculatorLead
        {
            Id = leadId,
            Name = "Uncontacted Client",
            Email = "uncontacted@test.bg",
            Phone = "+359888123456",
            IsContacted = false
        };

        _leadRepoMock.Setup(r => r.GetLeadsAsync()).ReturnsAsync(new List<CalculatorLead> { targetLead });
        _leadRepoMock.Setup(r => r.UpdateLeadAsync(It.IsAny<CalculatorLead>())).Returns(Task.CompletedTask);

        // Act - Render component and open modal
        var cut = Render<AdminLeads>();
        var inspectBtn = cut.Find("button.btn-inspect");
        inspectBtn.Click();

        // Assert - CRM Rating and Meeting menus should be hidden
        cut.FindAll(".rating-pill").Should().BeEmpty();
        cut.FindAll(".crm-prospect-card").Should().BeEmpty();
        cut.Find(".uncontacted-guide-notice").Should().NotBeNull();

        // Click "Mark as Contacted" toggle button
        var toggleBtn = cut.Find(".btn-toggle-contact");
        toggleBtn.Click();

        // Assert - CRM Rating and Meeting menus are now visible
        cut.FindAll(".rating-pill").Should().NotBeEmpty();
        cut.FindAll(".crm-prospect-card").Should().NotBeEmpty();
        cut.FindAll(".uncontacted-guide-notice").Should().BeEmpty();

        // Toggle back to uncontacted
        toggleBtn = cut.Find(".btn-toggle-contact");
        toggleBtn.Click();

        // Assert - Hidden again
        cut.FindAll(".rating-pill").Should().BeEmpty();
        cut.FindAll(".crm-prospect-card").Should().BeEmpty();
    }

    [Fact]
    public void AdminLeads_InspectLead_AndSaveCrmAssessment_ShouldUpdateLead()
    {
        // Arrange
        var leadId = Guid.NewGuid();
        var targetLead = new CalculatorLead
        {
            Id = leadId,
            Name = "Assessment Client",
            Email = "assess@test.bg",
            Phone = "+359888123456",
            ProspectRating = 5,
            CallOutcome = "Possible",
            MeetingStatus = "None",
            IsContacted = true
        };

        _leadRepoMock.Setup(r => r.GetLeadsAsync()).ReturnsAsync(new List<CalculatorLead> { targetLead });
        _leadRepoMock.Setup(r => r.UpdateLeadAsync(It.IsAny<CalculatorLead>())).Returns(Task.CompletedTask);

        // Act - Render component
        var cut = Render<AdminLeads>();

        // Find the inspect button to open modal
        var inspectBtn = cut.Find("button.btn-inspect");
        inspectBtn.Click();

        // Assert - Dossier modal opened
        var modal = cut.Find(".leads-modal-overlay");
        modal.Should().NotBeNull();

        // Click rating pill 9
        var ratingPills = cut.FindAll(".rating-pill");
        var pill9 = ratingPills.First(p => p.TextContent.Trim() == "9");
        pill9.Click();

        // Click "Willing to Work" outcome button
        var willingBtn = cut.Find(".outcome-option-btn.outcome-willing");
        willingBtn.Click();

        // Click quick add +7 days
        var add7Btn = cut.FindAll(".btn-quick-day").First(b => b.TextContent.Contains("Admin_Leads_QuickAdd1Week"));
        add7Btn.Click();

        // Save CRM notes
        var saveBtn = cut.Find(".crm-notes-actions button.bs-btn-primary");
        saveBtn.Click();

        // Assert - UpdateLeadAsync was called with new CRM assessment
        _leadRepoMock.Verify(r => r.UpdateLeadAsync(It.Is<CalculatorLead>(l =>
            l.Id == leadId &&
            l.ProspectRating == 9 &&
            l.CallOutcome == "WillingToWork" &&
            l.FollowUpDate.HasValue &&
            l.AdminNotes != null &&
            l.AdminNotes.Contains("[CRM_META]:"))),
            Times.Once);
    }

    [Fact]
    public void AdminLeads_MeetingAndFollowUpControls_ShouldPersistChanges()
    {
        // Arrange
        var leadId = Guid.NewGuid();
        var targetLead = new CalculatorLead
        {
            Id = leadId,
            Name = "Meeting Client",
            Email = "meetingtest@test.bg",
            Phone = "+359888555444",
            MeetingStatus = "None",
            IsContacted = true
        };

        _leadRepoMock.Setup(r => r.GetLeadsAsync()).ReturnsAsync(new List<CalculatorLead> { targetLead });
        _leadRepoMock.Setup(r => r.UpdateLeadAsync(It.IsAny<CalculatorLead>())).Returns(Task.CompletedTask);

        // Act
        var cut = Render<AdminLeads>();

        var inspectBtn = cut.Find("button.btn-inspect");
        inspectBtn.Click();

        // Change Meeting Status to Scheduled
        var meetingSelect = cut.FindAll("select.leads-select-input")[0];
        meetingSelect.Change("Scheduled");

        // Change Readiness Timeline to 1_month
        var timelineSelect = cut.FindAll("select.leads-select-input")[1];
        timelineSelect.Change("1_month");

        // Save
        var saveBtn = cut.Find(".crm-notes-actions button.bs-btn-primary");
        saveBtn.Click();

        // Assert
        _leadRepoMock.Verify(r => r.UpdateLeadAsync(It.Is<CalculatorLead>(l =>
            l.Id == leadId &&
            l.MeetingStatus == "Scheduled" &&
            l.ReadyToStartTimeline == "1_month")),
            Times.Once);
    }

    [Fact]
    public void AdminLeads_WhenClientIsMarkedUncontactedAndSaved_ClearsCrmAssessmentProperties()
    {
        // Arrange
        var leadId = Guid.NewGuid();
        var targetLead = new CalculatorLead
        {
            Id = leadId,
            Name = "Previously Contacted Client",
            Email = "prev@test.bg",
            Phone = "+359888999888",
            ProspectRating = 8,
            CallOutcome = "WillingToWork",
            MeetingStatus = "Scheduled",
            MeetingDate = DateTime.UtcNow.AddDays(1),
            ReadyToStartTimeline = "immediately",
            FollowUpDate = DateTime.UtcNow.Date.AddDays(7),
            IsContacted = true
        };

        _leadRepoMock.Setup(r => r.GetLeadsAsync()).ReturnsAsync(new List<CalculatorLead> { targetLead });
        _leadRepoMock.Setup(r => r.UpdateLeadAsync(It.IsAny<CalculatorLead>())).Returns(Task.CompletedTask);

        // Act
        var cut = Render<AdminLeads>();

        var inspectBtn = cut.Find("button.btn-inspect");
        inspectBtn.Click();

        // Mark as Uncontacted
        var toggleBtn = cut.Find(".btn-toggle-contact");
        toggleBtn.Click();

        // Save
        var saveBtn = cut.Find(".crm-notes-actions button.bs-btn-primary");
        saveBtn.Click();

        // Assert - CRM assessment and meeting fields are cleared
        _leadRepoMock.Verify(r => r.UpdateLeadAsync(It.Is<CalculatorLead>(l =>
            l.Id == leadId &&
            l.IsContacted == false &&
            l.ContactedAt == null &&
            l.ProspectRating == null &&
            l.CallOutcome == null &&
            l.MeetingStatus == null &&
            l.MeetingDate == null &&
            l.MeetingNotes == null &&
            l.ReadyToStartTimeline == null &&
            l.FollowUpDate == null)),
            Times.Once);
    }
}

public class TestAdminAuthStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "AdminUser"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuth");

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }
}
