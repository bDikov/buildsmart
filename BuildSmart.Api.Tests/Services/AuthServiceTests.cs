using System;
using System.Threading.Tasks;
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
    public class AuthServiceTests
    {
        private readonly DbContextOptions<AppDbContext> _dbOptions;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IConfiguration> _mockConfiguration;

        public AuthServiceTests()
        {
            _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _mockEmailService = new Mock<IEmailService>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Setup mock configuration for JWT
            _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("SuperSecretKeyOfLength32OrGreater!");
            _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("BuildSmart");
            _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("BuildSmart");
        }

        [Fact]
        public async Task GenerateJwtTokenForExternalLogin_ShouldCreateNewUserAndReturnIsNewUserTrue_WhenUserDoesNotExist()
        {
            // Arrange
            using var context = new AppDbContext(_dbOptions);
            var uow = new UnitOfWork(context);
            var authService = new AuthService(uow, _mockConfiguration.Object, _mockEmailService.Object);

            var email = "newuser@example.com";
            var name = "John Doe";

            // Act
            var (token, isNewUser) = await authService.GenerateJwtTokenForExternalLogin(email, name, "http://pic.com/1.jpg");

            // Assert
            token.Should().NotBeNullOrEmpty();
            isNewUser.Should().BeTrue();

            // Verify in DB
            var dbUser = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
            dbUser.Should().NotBeNull();
            dbUser.FirstName.Should().Be("John");
            dbUser.LastName.Should().Be("Doe");
            dbUser.ProfilePictureUrl.Should().Be("http://pic.com/1.jpg");
            dbUser.Role.Should().Be(UserRoleTypes.Homeowner);
        }

        [Fact]
        public async Task GenerateJwtTokenForExternalLogin_ShouldReturnIsNewUserFalse_WhenUserAlreadyExists()
        {
            // Arrange
            using var context = new AppDbContext(_dbOptions);
            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Jane",
                LastName = "Smith",
                Email = "existing@example.com",
                HashedPassword = "hashedpassword",
                Role = UserRoleTypes.Homeowner,
                IsEmailVerified = true
            };
            context.Users.Add(existingUser);
            await context.SaveChangesAsync();

            var uow = new UnitOfWork(context);
            var authService = new AuthService(uow, _mockConfiguration.Object, _mockEmailService.Object);

            // Act
            var (token, isNewUser) = await authService.GenerateJwtTokenForExternalLogin("existing@example.com", "Jane Smith");

            // Assert
            token.Should().NotBeNullOrEmpty();
            isNewUser.Should().BeFalse();
        }
    }
}
