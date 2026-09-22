using BuildSmart.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BuildSmart.Api.Tests;

public class TelegramBotServiceTests
{
    [Fact]
    public async Task SendNotificationAsync_ShouldReturnFalse_WhenDisabled()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Telegram:Enabled"] = "false",
            ["Telegram:BotToken"] = "test-token",
            ["Telegram:ChatId"] = "12345"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var mockLogger = new Mock<ILogger<TelegramBotService>>();
        var httpClient = new HttpClient();

        var service = new TelegramBotService(httpClient, config, mockLogger.Object);

        // Act
        var result = await service.SendNotificationAsync("Hello World");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendNotificationAsync_ShouldSendPostRequest_WhenEnabled()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Telegram:Enabled"] = "true",
            ["Telegram:BotToken"] = "123456:ABC-DEF",
            ["Telegram:ChatId"] = "987654"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var mockLogger = new Mock<ILogger<TelegramBotService>>();

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("123456:ABC-DEF/sendMessage")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"ok\":true}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new TelegramBotService(httpClient, config, mockLogger.Object);

        // Act
        var result = await service.SendNotificationAsync("Test alert message");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendLeadAlertAsync_ShouldCallTelegramApi_WithProjectDetails()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Telegram:Enabled"] = "true",
            ["Telegram:BotToken"] = "test-token",
            ["Telegram:ChatId"] = "123"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var mockLogger = new Mock<ILogger<TelegramBotService>>();

        HttpRequestMessage? capturedRequest = null;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ok\":true}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new TelegramBotService(httpClient, config, mockLogger.Object);
        var projectId = Guid.NewGuid();

        // Act
        var result = await service.SendLeadAlertAsync(
            projectId,
            "Ремонт на баня",
            "Иван Иванов",
            "+359888123456",
            "ivan@test.bg",
            "София",
            "AI препоръка: 12 кв.м"
        );

        // Assert
        result.Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        var requestBody = await capturedRequest!.Content!.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(requestBody);
        var text = doc.RootElement.GetProperty("text").GetString();
        text.Should().Contain("Ремонт на баня");
        text.Should().Contain("Иван Иванов");
        text.Should().Contain(projectId.ToString());
    }
}
