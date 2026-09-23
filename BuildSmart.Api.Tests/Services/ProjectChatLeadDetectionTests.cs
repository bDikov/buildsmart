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

namespace BuildSmart.Api.Tests.Services;

public class ProjectChatLeadDetectionTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<INotificationService> _mockNotification;
    private readonly Mock<IActiveProjectChatTracker> _mockTracker;
    private readonly Mock<ITelegramBotService> _mockTelegram;
    private readonly Mock<IAiService> _mockAiService;
    private readonly Mock<ICalculatorLeadRepository> _mockLeadRepository;
    private readonly ProjectChatService _service;

    public ProjectChatLeadDetectionTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockNotification = new Mock<INotificationService>();
        _mockTracker = new Mock<IActiveProjectChatTracker>();
        _mockTelegram = new Mock<ITelegramBotService>();
        _mockAiService = new Mock<IAiService>();
        _mockLeadRepository = new Mock<ICalculatorLeadRepository>();

        _service = new ProjectChatService(
            _mockUow.Object,
            _mockNotification.Object,
            _mockTracker.Object,
            _mockTelegram.Object,
            _mockAiService.Object,
            estimatorCalculator: null,
            logger: null,
            calculatorLeadRepository: _mockLeadRepository.Object
        );
    }

    [Fact]
    public void ExtractValidPhones_ShouldExtractBulgarianMobileNumbersFromText()
    {
        var text = "Здравейте, телефонът ми е 0888 123 456, казвам се Иван.";
        var phones = BulgarianPhoneValidator.ExtractValidPhones(text);

        phones.Should().ContainSingle();
        phones[0].Should().Be("0888 123 456");
    }

    [Fact]
    public void ExtractValidPhones_ShouldExtractInternationalFormatNumber()
    {
        var text = "Можете да се свържете с мен на +359 89 911 2233 през деня.";
        var phones = BulgarianPhoneValidator.ExtractValidPhones(text);

        phones.Should().ContainSingle();
        phones[0].Should().Be("0899 112 233");
    }

    [Fact]
    public void ExtractEmails_ShouldExtractValidEmailFromText()
    {
        var text = "Пишете ми на ivan.petrov@buildsmart.bg за повече детайли.";
        var emails = BulgarianPhoneValidator.ExtractEmails(text);

        emails.Should().ContainSingle();
        emails[0].Should().Be("ivan.petrov@buildsmart.bg");
    }

    [Fact]
    public async Task SendMessageAsync_WhenClientProvidesPhone_ShouldEnrichUserAndTriggerLeadAlert()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var homeownerId = Guid.NewGuid();
        var messageText = "Искам оферта за ремонт на апартамент. Телефонът ми е 0888 777 888.";

        var project = new Project
        {
            Id = projectId,
            HomeownerId = homeownerId,
            Title = "Ремонт Младост",
            Description = "Цялостен ремонт"
        };

        var homeownerUser = new User
        {
            Id = homeownerId,
            FirstName = "Георги",
            LastName = "Иванов",
            PhoneNumber = null,
            Email = "georgi@example.com",
            Role = UserRoleTypes.Homeowner
        };

        _mockUow.Setup(u => u.Projects.GetByIdAsync(projectId)).ReturnsAsync(project);
        _mockUow.Setup(u => u.Users.GetByIdAsync(homeownerId)).ReturnsAsync(homeownerUser);
        _mockUow.Setup(u => u.Users.GetQueryable()).Returns(new List<User>().BuildMockDbSet().Object);
        _mockUow.Setup(u => u.ProjectMessages.AddAsync(It.IsAny<ProjectMessage>())).Returns(Task.CompletedTask);
        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockLeadRepository.Setup(r => r.AddLeadAsync(It.IsAny<CalculatorLead>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.SendMessageAsync(projectId, homeownerId, messageText);

        // Assert
        result.Should().NotBeNull();
        homeownerUser.PhoneNumber.Should().Be("0888 777 888");

        _mockTelegram.Verify(t => t.SendLeadAlertAsync(
            projectId,
            "Ремонт Младост",
            "Георги Иванов",
            "0888 777 888",
            "georgi@example.com",
            "София",
            It.Is<string>(s => s.Contains("0888 777 888")),
            It.IsAny<CancellationToken>()
        ), Times.Once);

        _mockLeadRepository.Verify(r => r.AddLeadAsync(It.Is<CalculatorLead>(l => 
            l.Phone == "0888 777 888" && 
            l.Name == "Георги Иванов" &&
            l.Scope == "ai_chat"
        )), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_WhenGuestProvidesNameAndPhone_ShouldEnrichNameAndPhone()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var guestId = Guid.NewGuid();
        var messageText = "Казвам се Стефан Стоянов, тел. 0878 123 456, интересува ме баня.";

        var project = new Project
        {
            Id = projectId,
            HomeownerId = guestId,
            Title = "Support Chat",
            Description = "Anonymous session"
        };

        var guestUser = new User
        {
            Id = guestId,
            FirstName = "Guest",
            LastName = "User",
            Email = $"guest_{Guid.NewGuid():N}@buildsmart.guest",
            PhoneNumber = null,
            Role = UserRoleTypes.Homeowner
        };

        _mockUow.Setup(u => u.Projects.GetByIdAsync(projectId)).ReturnsAsync(project);
        _mockUow.Setup(u => u.Users.GetByIdAsync(guestId)).ReturnsAsync(guestUser);
        _mockUow.Setup(u => u.Users.GetQueryable()).Returns(new List<User>().BuildMockDbSet().Object);
        _mockUow.Setup(u => u.ProjectMessages.AddAsync(It.IsAny<ProjectMessage>())).Returns(Task.CompletedTask);
        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockLeadRepository.Setup(r => r.AddLeadAsync(It.IsAny<CalculatorLead>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.SendMessageAsync(projectId, guestId, messageText);

        // Assert
        result.Should().NotBeNull();
        guestUser.FirstName.Should().Be("Стефан");
        guestUser.LastName.Should().Be("Стоянов");
        guestUser.PhoneNumber.Should().Be("0878 123 456");

        _mockTelegram.Verify(t => t.SendLeadAlertAsync(
            projectId,
            "Support Chat",
            "Стефан Стоянов",
            "0878 123 456",
            null,
            "София",
            It.Is<string>(s => s.Contains("Стефан Стоянов")),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}
