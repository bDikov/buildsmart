using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Application.Services;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BuildSmart.Api.Tests;

public class ProjectChatServiceAiTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<INotificationService> _mockNotification;
    private readonly Mock<IActiveProjectChatTracker> _mockTracker;
    private readonly Mock<ITelegramBotService> _mockTelegram;
    private readonly Mock<IAiService> _mockAiService;
    private readonly ProjectChatService _service;

    public ProjectChatServiceAiTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockNotification = new Mock<INotificationService>();
        _mockTracker = new Mock<IActiveProjectChatTracker>();
        _mockTelegram = new Mock<ITelegramBotService>();
        _mockAiService = new Mock<IAiService>();

        _service = new ProjectChatService(
            _mockUow.Object,
            _mockNotification.Object,
            _mockTracker.Object,
            _mockTelegram.Object,
            _mockAiService.Object
        );
    }

    [Fact]
    public async Task SendMessageAsync_ShouldTriggerTelegramAlert_WhenHomeownerSendsMessage()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var homeownerId = Guid.NewGuid();
        var messageText = "Кога може да започнете ремонта?";

        var project = new Project
        {
            Id = projectId,
            HomeownerId = homeownerId,
            Title = "Основен ремонт",
            Description = "Описание"
        };

        var homeownerUser = new User
        {
            Id = homeownerId,
            FirstName = "Георги",
            LastName = "Димитров",
            PhoneNumber = "0888123456",
            Role = UserRoleTypes.Homeowner
        };

        _mockUow.Setup(u => u.Projects.GetByIdAsync(projectId)).ReturnsAsync(project);
        _mockUow.Setup(u => u.Users.GetByIdAsync(homeownerId)).ReturnsAsync(homeownerUser);
        _mockUow.Setup(u => u.Users.GetQueryable()).Returns(new List<User>().BuildMockDbSet().Object);
        _mockUow.Setup(u => u.ProjectMessages.AddAsync(It.IsAny<ProjectMessage>())).Returns(Task.CompletedTask);
        _mockUow.Setup(u => u.ProjectMessages.GetMessagesPaginatedAsync(projectId, 0, 2))
            .ReturnsAsync(new List<ProjectMessage> { new ProjectMessage { ProjectId = projectId }, new ProjectMessage { ProjectId = projectId } });

        // Act
        var result = await _service.SendMessageAsync(projectId, homeownerId, messageText);

        // Assert
        result.Should().NotBeNull();
        _mockTelegram.Verify(t => t.SendChatMessageAlertAsync(
            projectId,
            "Основен ремонт",
            "Георги Димитров",
            messageText,
            "0888123456",
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldInvokeAiService_OnFirstHomeownerMessage()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var homeownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var messageText = "Здравейте, интересува ме цена за шпакловка.";

        var project = new Project
        {
            Id = projectId,
            HomeownerId = homeownerId,
            Title = "Апартамент Лозенец",
            Description = "Шпакловка и боя",
            LanguageCode = "bg"
        };

        var homeownerUser = new User
        {
            Id = homeownerId,
            FirstName = "Мария",
            LastName = "Иванова",
            Role = UserRoleTypes.Homeowner
        };

        var adminUser = new User
        {
            Id = adminId,
            FirstName = "Admin",
            LastName = "Support",
            Role = UserRoleTypes.Admin
        };

        _mockUow.Setup(u => u.Projects.GetByIdAsync(projectId)).ReturnsAsync(project);
        _mockUow.Setup(u => u.Users.GetByIdAsync(homeownerId)).ReturnsAsync(homeownerUser);
        _mockUow.Setup(u => u.Users.GetQueryable()).Returns(new List<User> { adminUser }.BuildMockDbSet().Object);
        _mockUow.Setup(u => u.ProjectMessages.AddAsync(It.IsAny<ProjectMessage>())).Returns(Task.CompletedTask);
        // Only 1 message in history (the message currently sent)
        _mockUow.Setup(u => u.ProjectMessages.GetMessagesPaginatedAsync(projectId, 0, 2))
            .ReturnsAsync(new List<ProjectMessage> { new ProjectMessage { ProjectId = projectId } });

        _mockAiService.Setup(a => a.GenerateChatReplyAsync(
            It.IsAny<string>(),
            messageText,
            "bg",
            It.IsAny<CancellationToken>()
        )).ReturnsAsync("Здравейте, Мария! Разглеждаме проекта за шпакловка и боя.");

        // Act
        var result = await _service.SendMessageAsync(projectId, homeownerId, messageText);

        // Assert
        result.Should().NotBeNull();
        _mockAiService.Verify(a => a.GenerateChatReplyAsync(
            It.IsAny<string>(),
            messageText,
            "bg",
            It.IsAny<CancellationToken>()
        ), Times.Once);

        _mockNotification.Verify(n => n.NotifyProjectGroupAsync(
            projectId,
            "ReceiveProjectMessage",
            It.Is<object>(o => o != null && o.GetType().GetProperty("MessageText")!.GetValue(o)!.ToString()!.Contains("Здравейте, Мария!"))
        ), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldNotInvokeAiService_WhenHumanTakeoverActive()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var homeownerId = Guid.NewGuid();
        var messageText = "Имате ли свободен час за оглед?";

        var project = new Project
        {
            Id = projectId,
            HomeownerId = homeownerId,
            Title = "Ремонт апартамент",
            Description = "Боядисване"
        };

        var homeownerUser = new User
        {
            Id = homeownerId,
            FirstName = "Петър",
            LastName = "Стоянов",
            Role = UserRoleTypes.Homeowner
        };

        _mockUow.Setup(u => u.Projects.GetByIdAsync(projectId)).ReturnsAsync(project);
        _mockUow.Setup(u => u.Users.GetByIdAsync(homeownerId)).ReturnsAsync(homeownerUser);
        _mockUow.Setup(u => u.Users.GetQueryable()).Returns(new List<User>().BuildMockDbSet().Object);
        _mockUow.Setup(u => u.ProjectMessages.AddAsync(It.IsAny<ProjectMessage>())).Returns(Task.CompletedTask);

        // Act: Manually activate human takeover (simulating admin just sent a message)
        ProjectChatService.SetHumanTakeover(projectId, true, TimeSpan.FromMinutes(30));

        var result = await _service.SendMessageAsync(projectId, homeownerId, messageText);

        // Assert
        result.Should().NotBeNull();
        // AI service should NEVER be invoked because a human admin is active
        _mockAiService.Verify(a => a.GenerateChatReplyAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()
        ), Times.Never);

        // But Telegram alert should STILL be sent to the admin so the admin can reply!
        _mockTelegram.Verify(t => t.SendChatMessageAlertAsync(
            projectId,
            "Ремонт апартамент",
            "Петър Стоянов",
            messageText,
            null,
            It.IsAny<CancellationToken>()
        ), Times.Once);

        // Cleanup
        ProjectChatService.SetHumanTakeover(projectId, false);
    }
}

