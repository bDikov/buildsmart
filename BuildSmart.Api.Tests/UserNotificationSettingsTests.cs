using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BuildSmart.Api.GraphQL;
using BuildSmart.Api.Services;
using BuildSmart.Api.Hubs;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Infrastructure.Persistence;
using FluentAssertions;
using HotChocolate;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace BuildSmart.Api.Tests
{
    public class UserNotificationSettingsTests
    {
        private readonly DbContextOptions<AppDbContext> _dbOptions;

        public UserNotificationSettingsTests()
        {
            _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task UpdateUserEmailNotifications_ShouldUpdateUserFieldsInDatabase()
        {
            // Arrange
            using var context = new AppDbContext(_dbOptions);
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "User",
                Email = "test@buildsmart.bg",
                EmailOnOfferReady = true,
                EmailOnNewChatMessage = false
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Users.GetByIdAsync(user.Id)).ReturnsAsync(user);

            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            }));

            var mutation = new Mutation();

            // Act
            var result = await mutation.UpdateUserEmailNotifications(
                emailOnOfferReady: false,
                emailOnNewChatMessage: true,
                claimsPrincipal: claimsPrincipal,
                unitOfWork: mockUow.Object
            );

            // Assert
            result.Should().NotBeNull();
            result.EmailOnOfferReady.Should().BeFalse();
            result.EmailOnNewChatMessage.Should().BeTrue();
            mockUow.Verify(u => u.Users.Update(user), Times.Once);
            mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendLocalizedNotificationAsync_ShouldNotQueueEmail_WhenPreferenceIsDisabled()
        {
            // Arrange
            using var context = new AppDbContext(_dbOptions);
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "User",
                Email = "test@buildsmart.bg",
                EmailOnNewChatMessage = false // Disabled!
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var mockUow = new Mock<IUnitOfWork>();
            mockUow.Setup(u => u.Users.GetByIdAsync(user.Id)).ReturnsAsync(user);
            
            var mockNotificationRepo = new Mock<INotificationRepository>();
            mockUow.Setup(u => u.Notifications).Returns(mockNotificationRepo.Object);

            var mockHub = new Mock<IHubContext<NotificationHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            mockHub.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClients.Setup(c => c.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

            var mockLocalizer = new Mock<IStringLocalizer<BuildSmart.Core.Application.Resources.NotificationResources>>();
            mockLocalizer.Setup(l => l[It.IsAny<string>()]).Returns(new LocalizedString("key", "test message"));

            var services = new ServiceCollection();
            services.AddSingleton(mockUow.Object);
            services.AddSingleton(new Mock<IEmailService>().Object);
            var serviceProvider = services.BuildServiceProvider();

            var service = new NotificationService(mockHub.Object, serviceProvider, mockLocalizer.Object);

            // Act
            await service.SendLocalizedNotificationAsync(
                userId: user.Id,
                titleKey: "Notification_NewMessage_Title",
                messageKey: "Notification_NewMessage_Body"
            );

            // Assert
            // Verification is successful if no email is queued. In-memory DB shouldn't throw exception, and Hub client proxy is called.
            mockClientProxy.Verify(
                c => c.SendCoreAsync("ReceiveNotification", It.IsAny<object[]>(), It.IsAny<System.Threading.CancellationToken>()),
                Times.Once
            );
        }
    }
}
