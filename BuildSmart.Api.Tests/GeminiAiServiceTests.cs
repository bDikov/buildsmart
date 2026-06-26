using System;
using System.Text.Json;
using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BuildSmart.Infrastructure.Services;

namespace BuildSmart.Api.Tests
{
    public class GeminiAiServiceTests
    {
        [Fact]
        public void EscapeRawControlCharacters_ShouldEscapeRawNewlinesAndTabsInsideJsonStringValues()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Gemini:ApiKey"]).Returns("dummy-key");
            var loggerMock = new Mock<ILogger<GeminiAiService>>();
            var service = new GeminiAiService(configMock.Object, loggerMock.Object);

            var method = typeof(GeminiAiService).GetMethod("EscapeRawControlCharacters", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(method);

            // This JSON has raw 0x0A (newline) and raw 0x09 (tab) characters inside the "scopeMarkdown" value
            string rawJsonInput = "{\n  \"scopeMarkdown\": \"Line 1\nLine 2\twith tab\",\n  \"tasks\": []\n}";

            // Act
            var cleanedJson = (string?)method.Invoke(service, new object[] { rawJsonInput });
            Assert.NotNull(cleanedJson);

            // Assert
            // 1. Verify that deserialization succeeds
            var deserialized = JsonSerializer.Deserialize<JsonDocument>(cleanedJson);
            Assert.NotNull(deserialized);

            // 2. Verify values are correct
            var scope = deserialized.RootElement.GetProperty("scopeMarkdown").GetString();
            Assert.Equal("Line 1\nLine 2\twith tab", scope);
        }

        [Fact]
        public void EscapeRawControlCharacters_ShouldNotAlterWhitespaceOutsideJsonStrings()
        {
            // Arrange
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Gemini:ApiKey"]).Returns("dummy-key");
            var loggerMock = new Mock<ILogger<GeminiAiService>>();
            var service = new GeminiAiService(configMock.Object, loggerMock.Object);

            var method = typeof(GeminiAiService).GetMethod("EscapeRawControlCharacters", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(method);

            string formattedJson = "{\n  \"key\": \"value\"\n}";

            // Act
            var result = (string?)method.Invoke(service, new object[] { formattedJson });

            // Assert
            Assert.Equal(formattedJson, result);
        }
    }
}
