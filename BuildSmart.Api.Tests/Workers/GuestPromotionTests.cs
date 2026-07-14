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

namespace BuildSmart.Api.Tests.Workers;

public class GuestPromotionTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public GuestPromotionTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _mockEmailService = new Mock<IEmailService>();
        _mockConfiguration = new Mock<IConfiguration>();
    }

    [Fact]
    public async Task PromoteGuestToUserAsync_ShouldSuccessfullyPromoteGuest_WhenDetailsAreValid()
    {
        // Arrange
        using var context = new AppDbContext(_dbOptions);
        var guestUserId = Guid.NewGuid();
        var guestUser = new User
        {
            Id = guestUserId,
            Email = "guest_123@buildsmart.guest",
            FirstName = "Guest",
            LastName = "User",
            Role = UserRoleTypes.Homeowner,
            HashedPassword = null,
            IsEmailVerified = false
        };

        // Create a project for the guest
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = "Support Chat",
            Description = "Support chat description",
            HomeownerId = guestUserId
        };


        context.Users.Add(guestUser);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var authService = new AuthService(uow, _mockConfiguration.Object, _mockEmailService.Object);

        // Act
        var promotedUser = await authService.PromoteGuestToUserAsync(
            guestUserId,
            "RealName",
            "RealLastName",
            "realuser@example.com",
            "NewSecurePassword123",
            "0888888888"
        );

        // Assert
        promotedUser.Should().NotBeNull();
        promotedUser.Email.Should().Be("realuser@example.com");
        promotedUser.FirstName.Should().Be("RealName");
        promotedUser.LastName.Should().Be("RealLastName");
        promotedUser.PhoneNumber.Should().Be("0888888888");
        promotedUser.HashedPassword.Should().NotBeNull();
        BCrypt.Net.BCrypt.Verify("NewSecurePassword123", promotedUser.HashedPassword).Should().BeTrue();
        promotedUser.IsEmailVerified.Should().BeFalse();
        promotedUser.EmailVerificationToken.Should().NotBeNull().And.HaveLength(6);

        // Verify the project is still linked to the promoted user
        var dbProject = await context.Projects.FindAsync(project.Id);
        dbProject.Should().NotBeNull();
        dbProject.HomeownerId.Should().Be(guestUserId);

        // Verify email was sent
        _mockEmailService.Verify(e => e.SendGenericEmailAsync(
            "realuser@example.com",
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task PromoteGuestToUserAsync_ShouldThrowException_WhenUserIsAlreadyStandardUser()
    {
        // Arrange
        using var context = new AppDbContext(_dbOptions);
        var userId = Guid.NewGuid();
        var standardUser = new User
        {
            Id = userId,
            Email = "already_standard@example.com",
            FirstName = "Standard",
            LastName = "User",
            HashedPassword = "some_hashed_password"
        };
        context.Users.Add(standardUser);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var authService = new AuthService(uow, _mockConfiguration.Object, _mockEmailService.Object);

        // Act & Assert
        Func<Task> act = async () => await authService.PromoteGuestToUserAsync(
            userId,
            "RealName",
            "RealLastName",
            "newemail@example.com",
            "NewPassword123"
        );

        await act.Should().ThrowAsync<Exception>().WithMessage("The user is already a standard user or not a guest.");
    }

    [Fact]
    public async Task PromoteGuestToUserAsync_ShouldThrowException_WhenNewEmailIsAlreadyRegistered()
    {
        // Arrange
        using var context = new AppDbContext(_dbOptions);
        var guestUserId = Guid.NewGuid();
        var guestUser = new User
        {
            Id = guestUserId,
            Email = "guest_456@buildsmart.guest",
            FirstName = "Guest",
            LastName = "User",
            HashedPassword = null
        };
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "taken@example.com",
            FirstName = "Existing",
            LastName = "User",
            HashedPassword = "hashed_password"
        };
        context.Users.AddRange(guestUser, existingUser);
        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var authService = new AuthService(uow, _mockConfiguration.Object, _mockEmailService.Object);

        // Act & Assert
        Func<Task> act = async () => await authService.PromoteGuestToUserAsync(
            guestUserId,
            "RealName",
            "RealLastName",
            "taken@example.com",
            "Password123"
        );

        await act.Should().ThrowAsync<Exception>().WithMessage("User with this email already exists.");
    }
}
