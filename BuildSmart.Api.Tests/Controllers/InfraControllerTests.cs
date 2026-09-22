using BuildSmart.Api.Controllers;
using BuildSmart.Core.Application.DTOs;
using BuildSmart.Core.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BuildSmart.Api.Tests.Controllers;

public class InfraControllerTests
{
    private readonly Mock<IInfraService> _mockInfraService;
    private readonly Mock<ILogger<InfraController>> _mockLogger;
    private readonly InfraController _controller;

    public InfraControllerTests()
    {
        _mockInfraService = new Mock<IInfraService>();
        _mockLogger = new Mock<ILogger<InfraController>>();

        _controller = new InfraController(_mockInfraService.Object, _mockLogger.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetStatus_ShouldReturnOk_WithStatusDto()
    {
        // Arrange
        var fakeStatus = new InfraStatusDto
        {
            DatabaseConnected = true,
            DatabaseLatencyMs = "3 ms",
            MemoryUsageMb = 120.5,
            Environment = "Test"
        };
        _mockInfraService.Setup(s => s.GetSystemStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeStatus);

        // Act
        var result = await _controller.GetStatus();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeOfType<InfraStatusDto>().Subject;
        data.DatabaseConnected.Should().BeTrue();
        data.DatabaseLatencyMs.Should().Be("3 ms");
    }

    [Fact]
    public async Task ReceiveSentryWebhook_ShouldReturnOk_WhenValidJson()
    {
        // Arrange
        var webhookJson = @"{ ""message"": ""Unhandled NullReferenceException in PricingEngine.cs"" }";
        _controller.ControllerContext.HttpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(webhookJson));

        _mockInfraService.Setup(s => s.ProcessSentryWebhookAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ReceiveSentryWebhook();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Restart_ShouldReturnOk_WhenTriggered()
    {
        // Arrange
        _mockInfraService.Setup(s => s.RestartServiceAsync("api", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Restart("api");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockInfraService.Verify(s => s.RestartServiceAsync("api", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TriggerFix_ShouldReturnOk_WhenServiceDispatches()
    {
        // Arrange
        var request = new TriggerFixRequest
        {
            ErrorTitle = "NullReferenceException",
            ErrorMessage = "Object reference not set",
            FilePath = "BuildSmart.Infrastructure/Services/PricingEngine.cs",
            LineNumber = 142
        };

        _mockInfraService.Setup(s => s.TriggerSelfHealingWorkflowAsync(
            request.ErrorTitle,
            request.ErrorMessage,
            request.StackTrace,
            request.FilePath,
            request.LineNumber,
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(true);

        // Act
        var result = await _controller.TriggerFix(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockInfraService.Verify(s => s.TriggerSelfHealingWorkflowAsync(
            request.ErrorTitle,
            request.ErrorMessage,
            request.StackTrace,
            request.FilePath,
            request.LineNumber,
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}
