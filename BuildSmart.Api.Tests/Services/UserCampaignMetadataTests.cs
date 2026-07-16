using System;
using System.Threading.Tasks;
using BuildSmart.Api.GraphQL;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Application.Services;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace BuildSmart.Api.Tests.Services
{
    public class UserCampaignMetadataTests
    {
        private readonly DbContextOptions<AppDbContext> _dbOptions;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IEmailService> _mockEmailService;

        public UserCampaignMetadataTests()
        {
            _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _mockConfiguration = new Mock<IConfiguration>();
            _mockEmailService = new Mock<IEmailService>();
        }

        [Fact]
        public async Task SaveUserCampaignMetadata_ShouldCreateMetadataRecord_WhenUserExistsAndUTMsAreValid()
        {
            // Arrange
            using var context = new AppDbContext(_dbOptions);
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FirstName = "Attributed",
                LastName = "User",
                Email = "attributed@example.com",
                Role = UserRoleTypes.Homeowner,
                IsEmailVerified = true
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var mutation = new Mutation();

            // Act
            var result = await mutation.SaveUserCampaignMetadata(
                userId,
                "google",
                "cpc",
                "autumn_campaign",
                "banner_ad",
                "renovation_keywords",
                context
            );

            // Assert
            result.Should().BeTrue();

            var metadata = await context.UserCampaignMetadata
                .FirstOrDefaultAsync(m => m.UserId == userId);

            metadata.Should().NotBeNull();
            metadata.UtmSource.Should().Be("google");
            metadata.UtmMedium.Should().Be("cpc");
            metadata.UtmCampaign.Should().Be("autumn_campaign");
            metadata.UtmContent.Should().Be("banner_ad");
            metadata.UtmTerm.Should().Be("renovation_keywords");
        }

        [Fact]
        public async Task SaveUserCampaignMetadata_ShouldNotDuplicate_WhenCalledMultipleTimesForSameUser()
        {
            // Arrange
            using var context = new AppDbContext(_dbOptions);
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FirstName = "Single",
                LastName = "Attributed",
                Email = "single@example.com",
                Role = UserRoleTypes.Homeowner,
                IsEmailVerified = true
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var mutation = new Mutation();

            // Act - call twice
            var result1 = await mutation.SaveUserCampaignMetadata(userId, "google", "cpc", "camp1", null, null, context);
            var result2 = await mutation.SaveUserCampaignMetadata(userId, "google", "cpc", "camp2_ignored", null, null, context);

            // Assert
            result1.Should().BeTrue();
            result2.Should().BeTrue();

            var count = await context.UserCampaignMetadata.CountAsync(m => m.UserId == userId);
            count.Should().Be(1);

            var metadata = await context.UserCampaignMetadata.FirstAsync(m => m.UserId == userId);
            metadata.UtmCampaign.Should().Be("camp1"); // Should retain the first recorded campaign
        }

        [Fact]
        public async Task RegisterUser_ShouldCreateCampaignMetadataRecord_WhenUTMParametersArePassed()
        {
            // Arrange
            using var context = new AppDbContext(_dbOptions);
            var uow = new UnitOfWork(context);
            var authService = new AuthService(uow, _mockConfiguration.Object, _mockEmailService.Object);

            var mutation = new Mutation();

            // Act
            var registeredUser = await mutation.RegisterUser(
                "Jane",
                "Doe",
                "jane.doe@example.com",
                "SecurePassword123!",
                "0888123456",
                "facebook",
                "social",
                "facebook_campaign",
                "ad_image",
                null,
                authService,
                context
            );

            // Assert
            registeredUser.Should().NotBeNull();
            
            var metadata = await context.UserCampaignMetadata
                .FirstOrDefaultAsync(m => m.UserId == registeredUser.Id);

            metadata.Should().NotBeNull();
            metadata.UtmSource.Should().Be("facebook");
            metadata.UtmMedium.Should().Be("social");
            metadata.UtmCampaign.Should().Be("facebook_campaign");
            metadata.UtmContent.Should().Be("ad_image");
            metadata.UtmTerm.Should().BeNull();
        }
    }
}
