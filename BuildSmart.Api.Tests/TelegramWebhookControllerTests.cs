using BuildSmart.Api.Controllers;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BuildSmart.Api.Tests;

public class TelegramWebhookControllerTests
{
    private readonly Mock<IProjectChatService> _mockChatService;
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IAiService> _mockAiService;
    private readonly Mock<ILogger<TelegramWebhookController>> _mockLogger;

    public TelegramWebhookControllerTests()
    {
        _mockChatService = new Mock<IProjectChatService>();
        _mockUow = new Mock<IUnitOfWork>();
        _mockAiService = new Mock<IAiService>();
        _mockLogger = new Mock<ILogger<TelegramWebhookController>>();
    }

    [Fact]
    public async Task ReceiveWebhook_ShouldReturnForbid_WhenSecretMismatch()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Telegram:WebhookSecretToken"] = "my_expected_secret"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        var controller = new TelegramWebhookController(
            config,
            _mockChatService.Object,
            _mockUow.Object,
            _mockAiService.Object,
            _mockLogger.Object
        );

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Telegram-Bot-Api-Secret-Token"] = "wrong_secret";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await controller.ReceiveWebhook();

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task ReceiveWebhook_ShouldForwardReplyToProjectChat_WhenPIDPresent()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var adminUser = new User
        {
            Id = adminId,
            FirstName = "Admin",
            LastName = "User",
            Role = UserRoleTypes.Admin
        };

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        _mockUow.Setup(u => u.Users.GetQueryable())
            .Returns(new List<User> { adminUser }.BuildMockDbSet().Object);

        var controller = new TelegramWebhookController(
            config,
            _mockChatService.Object,
            _mockUow.Object,
            _mockAiService.Object,
            _mockLogger.Object
        );

        var jsonPayload = $@"{{
            ""update_id"": 12345,
            ""message"": {{
                ""message_id"": 99,
                ""text"": ""Да, офертата включва транспорт."",
                ""reply_to_message"": {{
                    ""message_id"": 98,
                    ""text"": ""Ново съобщение по проект PID:{projectId}""
                }}
            }}
        }}";

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(jsonPayload));
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await controller.ReceiveWebhook();

        // Assert
        result.Should().BeOfType<OkResult>();
        _mockChatService.Verify(c => c.SendMessageAsync(
            projectId,
            adminId,
            "Да, офертата включва транспорт."
        ), Times.Once);
    }
}
