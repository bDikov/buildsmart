using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Application.Services;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using FluentAssertions;
using Moq;
using MockQueryable.Moq;
using Xunit;

namespace BuildSmart.Api.Tests;

public class ProjectChatServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<INotificationService> _mockNotification;
    private readonly Mock<IActiveProjectChatTracker> _mockTracker;
    private readonly ProjectChatService _service;

    public ProjectChatServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockNotification = new Mock<INotificationService>();
        _mockTracker = new Mock<IActiveProjectChatTracker>();
        _service = new ProjectChatService(_mockUow.Object, _mockNotification.Object, _mockTracker.Object);
    }

    [Fact]
    public async Task GetProjectMessagesAsync_ShouldThrowException_WhenProjectNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockUow.Setup(u => u.Projects.GetByIdAsync(projectId))
            .ReturnsAsync((Project?)null);

        // Act
        Func<Task> act = async () => await _service.GetProjectMessagesAsync(projectId, userId, 0, 10);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Project not found");
    }

    [Fact]
    public async Task GetProjectMessagesAsync_ShouldThrowException_WhenUserNotAuthorized()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var homeownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        
        var project = new Project
        {
            Id = projectId,
            HomeownerId = homeownerId,
            Title = "Test Project",
            Description = "Description"
        };

        var otherUser = new User
        {
            Id = otherUserId,
            Role = UserRoleTypes.Tradesman // Tradesman is not authorized to view homeowner/admin chat
        };

        _mockUow.Setup(u => u.Projects.GetByIdAsync(projectId)).ReturnsAsync(project);
        _mockUow.Setup(u => u.Users.GetByIdAsync(otherUserId)).ReturnsAsync(otherUser);

        // Act
        Func<Task> act = async () => await _service.GetProjectMessagesAsync(projectId, otherUserId, 0, 10);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Not authorized to view project messages.");
    }

    [Fact]
    public async Task GetProjectMessagesAsync_ShouldReturnMessages_WhenUserIsHomeowner()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var homeownerId = Guid.NewGuid();
        
        var project = new Project
        {
            Id = projectId,
            HomeownerId = homeownerId,
            Title = "Test Project",
            Description = "Description"
        };

        var homeownerUser = new User
        {
            Id = homeownerId,
            Role = UserRoleTypes.Homeowner
        };

        var expectedMessages = new List<ProjectMessage>
        {
            new ProjectMessage { Id = Guid.NewGuid(), ProjectId = projectId, SenderId = homeownerId, MessageText = "Hello" }
        };

        _mockUow.Setup(u => u.Projects.GetByIdAsync(projectId)).ReturnsAsync(project);
        _mockUow.Setup(u => u.Users.GetByIdAsync(homeownerId)).ReturnsAsync(homeownerUser);
        _mockUow.Setup(u => u.ProjectMessages.GetMessagesPaginatedAsync(projectId, 0, 10)).ReturnsAsync(expectedMessages);

        // Act
        var result = await _service.GetProjectMessagesAsync(projectId, homeownerId, 0, 10);

        // Assert
        result.Should().BeEquivalentTo(expectedMessages);
    }

    [Fact]
    public async Task GetProjectMessagesAsync_ShouldReturnMessages_WhenUserIsAdmin()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var homeownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        
        var project = new Project
        {
            Id = projectId,
            HomeownerId = homeownerId,
            Title = "Test Project",
            Description = "Description"
        };

        var adminUser = new User
        {
            Id = adminId,
            Role = UserRoleTypes.Admin
        };

        var expectedMessages = new List<ProjectMessage>
        {
            new ProjectMessage { Id = Guid.NewGuid(), ProjectId = projectId, SenderId = homeownerId, MessageText = "Hello" }
        };

        _mockUow.Setup(u => u.Projects.GetByIdAsync(projectId)).ReturnsAsync(project);
        _mockUow.Setup(u => u.Users.GetByIdAsync(adminId)).ReturnsAsync(adminUser);
        _mockUow.Setup(u => u.ProjectMessages.GetMessagesPaginatedAsync(projectId, 0, 10)).ReturnsAsync(expectedMessages);

        // Act
        var result = await _service.GetProjectMessagesAsync(projectId, adminId, 0, 10);

        // Assert
        result.Should().BeEquivalentTo(expectedMessages);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldAddMessageSaveAndNotify()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var messageText = "This is a chat message";
        
        var project = new Project
        {
            Id = projectId,
            HomeownerId = senderId,
            Title = "Test Project",
            Description = "Description"
        };

        var senderUser = new User
        {
            Id = senderId,
            FirstName = "John",
            LastName = "Doe",
            Role = UserRoleTypes.Homeowner
        };

        _mockUow.Setup(u => u.Projects.GetByIdAsync(projectId)).ReturnsAsync(project);
        _mockUow.Setup(u => u.Users.GetByIdAsync(senderId)).ReturnsAsync(senderUser);
        _mockUow.Setup(u => u.Users.GetQueryable()).Returns(new List<User>().BuildMockDbSet().Object);
        _mockUow.Setup(u => u.ProjectMessages.AddAsync(It.IsAny<ProjectMessage>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.SendMessageAsync(projectId, senderId, messageText);

        // Assert
        result.Should().NotBeNull();
        result.ProjectId.Should().Be(projectId);
        result.SenderId.Should().Be(senderId);
        result.MessageText.Should().Be(messageText);

        _mockUow.Verify(u => u.ProjectMessages.AddAsync(It.IsAny<ProjectMessage>()), Times.Once);
        _mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        _mockNotification.Verify(n => n.NotifyProjectGroupAsync(projectId, "ReceiveProjectMessage", It.IsAny<object>()), Times.Once);

        // Notification should NOT be sent because the sender is the homeowner
        _mockNotification.Verify(n => n.SendLocalizedNotificationAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldSendSupportReplyNotification_WhenSenderIsNotHomeownerAndHomeownerNotActive()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var homeownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var messageText = "Support response";

        var project = new Project
        {
            Id = projectId,
            HomeownerId = homeownerId,
            Title = "Test Project",
            Description = "Description"
        };

        var adminUser = new User
        {
            Id = adminId,
            FirstName = "Admin",
            LastName = "Support",
            Role = UserRoleTypes.Admin
        };

        _mockUow.Setup(u => u.Projects.GetByIdAsync(projectId)).ReturnsAsync(project);
        _mockUow.Setup(u => u.Users.GetByIdAsync(adminId)).ReturnsAsync(adminUser);
        _mockUow.Setup(u => u.ProjectMessages.AddAsync(It.IsAny<ProjectMessage>())).Returns(Task.CompletedTask);
        
        // Homeowner is NOT active in project chat
        _mockTracker.Setup(t => t.IsUserActiveInProject(homeownerId.ToString(), projectId.ToString()))
            .Returns(false);

        // Act
        var result = await _service.SendMessageAsync(projectId, adminId, messageText);

        // Assert
        result.Should().NotBeNull();
        _mockNotification.Verify(n => n.SendLocalizedNotificationAsync(
            homeownerId,
            "Notification_SupportReply_Title",
            "Notification_SupportReply_Body",
            It.Is<object[]>(args => args.Length == 1 && (string)args[0] == messageText),
            project.Id,
            "Project",
            It.IsAny<object>()
        ), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldNotSendSupportReplyNotification_WhenSenderIsNotHomeownerButHomeownerIsActive()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var homeownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var messageText = "Support response";

        var project = new Project
        {
            Id = projectId,
            HomeownerId = homeownerId,
            Title = "Test Project",
            Description = "Description"
        };

        var adminUser = new User
        {
            Id = adminId,
            FirstName = "Admin",
            LastName = "Support",
            Role = UserRoleTypes.Admin
        };

        _mockUow.Setup(u => u.Projects.GetByIdAsync(projectId)).ReturnsAsync(project);
        _mockUow.Setup(u => u.Users.GetByIdAsync(adminId)).ReturnsAsync(adminUser);
        _mockUow.Setup(u => u.ProjectMessages.AddAsync(It.IsAny<ProjectMessage>())).Returns(Task.CompletedTask);
        
        // Homeowner IS active in project chat
        _mockTracker.Setup(t => t.IsUserActiveInProject(homeownerId.ToString(), projectId.ToString()))
            .Returns(true);

        // Act
        var result = await _service.SendMessageAsync(projectId, adminId, messageText);

        // Assert
        result.Should().NotBeNull();
        _mockNotification.Verify(n => n.SendLocalizedNotificationAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_ShouldSendNotificationToAdmins_WhenSenderIsHomeownerAndAdminsNotActive()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var homeownerId = Guid.NewGuid();
        var adminId1 = Guid.NewGuid();
        var adminId2 = Guid.NewGuid();
        var messageText = "Hello support team";

        var project = new Project
        {
            Id = projectId,
            HomeownerId = homeownerId,
            Title = "Test Project",
            Description = "Description"
        };

        var homeownerUser = new User
        {
            Id = homeownerId,
            FirstName = "John",
            LastName = "Doe",
            Role = UserRoleTypes.Homeowner
        };

        var admins = new List<User>
        {
            new User { Id = adminId1, Role = UserRoleTypes.Admin },
            new User { Id = adminId2, Role = UserRoleTypes.Admin }
        };

        _mockUow.Setup(u => u.Projects.GetByIdAsync(projectId)).ReturnsAsync(project);
        _mockUow.Setup(u => u.Users.GetByIdAsync(homeownerId)).ReturnsAsync(homeownerUser);
        _mockUow.Setup(u => u.Users.GetQueryable()).Returns(admins.BuildMockDbSet().Object);
        _mockUow.Setup(u => u.ProjectMessages.AddAsync(It.IsAny<ProjectMessage>())).Returns(Task.CompletedTask);

        // Admin 1 is NOT active, Admin 2 IS active
        _mockTracker.Setup(t => t.IsUserActiveInProject(adminId1.ToString(), projectId.ToString())).Returns(false);
        _mockTracker.Setup(t => t.IsUserActiveInProject(adminId2.ToString(), projectId.ToString())).Returns(true);

        // Act
        var result = await _service.SendMessageAsync(projectId, homeownerId, messageText);

        // Assert
        result.Should().NotBeNull();

        // Notification should be sent to Admin 1
        _mockNotification.Verify(n => n.SendLocalizedNotificationAsync(
            adminId1,
            "Notification_NewMessage_Title",
            "Notification_NewMessage_Body",
            It.Is<object[]>(args => args.Length == 1 && (string)args[0] == messageText),
            project.Id,
            "Project",
            It.IsAny<object>()
        ), Times.Once);

        // Notification should NOT be sent to Admin 2
        _mockNotification.Verify(n => n.SendLocalizedNotificationAsync(
            adminId2,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<object[]>(),
            It.IsAny<Guid?>(),
            It.IsAny<string>(),
            It.IsAny<object>()
        ), Times.Never);
    }
}
