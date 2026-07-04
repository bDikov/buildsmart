using System;
using BuildSmart.Core.Application.Services;
using Xunit;

namespace BuildSmart.Api.Tests.Services;

public class UserPresenceServiceTests
{
    [Fact]
    public void IsUserOnline_ShouldReturnFalse_WhenUserHasNoConnections()
    {
        // Arrange
        var service = new UserPresenceService();
        var userId = Guid.NewGuid().ToString();

        // Act
        var result = service.IsUserOnline(userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UserConnected_ShouldMarkUserAsOnline()
    {
        // Arrange
        var service = new UserPresenceService();
        var userId = Guid.NewGuid().ToString();
        var connectionId = "conn1";

        // Act
        service.UserConnected(connectionId, userId);
        var result = service.IsUserOnline(userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void UserDisconnected_ShouldMarkUserAsOffline_WhenLastConnectionRemoved()
    {
        // Arrange
        var service = new UserPresenceService();
        var userId = Guid.NewGuid().ToString();
        var connectionId1 = "conn1";
        var connectionId2 = "conn2";

        // Act
        service.UserConnected(connectionId1, userId);
        service.UserConnected(connectionId2, userId);
        
        // Assert they are online
        Assert.True(service.IsUserOnline(userId));

        // Disconnect connection 1
        var removedUserId1 = service.UserDisconnected(connectionId1);
        Assert.Equal(userId, removedUserId1);
        Assert.True(service.IsUserOnline(userId)); // Still online because connectionId2 is active

        // Disconnect connection 2
        var removedUserId2 = service.UserDisconnected(connectionId2);
        Assert.Equal(userId, removedUserId2);
        Assert.False(service.IsUserOnline(userId)); // Offline now
    }

    [Fact]
    public void UserDisconnected_ShouldReturnNull_WhenConnectionIdDoesNotExist()
    {
        // Arrange
        var service = new UserPresenceService();

        // Act
        var result = service.UserDisconnected("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetUserActiveStatus_ShouldReturnOnline_WhenUserIsConnected()
    {
        // Arrange
        var service = new UserPresenceService();
        var userId = Guid.NewGuid().ToString();
        service.UserConnected("conn-123", userId);

        // Act
        var status = service.GetUserActiveStatus(userId, DateTime.UtcNow.AddHours(-48));

        // Assert
        Assert.Equal(BuildSmart.Core.Domain.Enums.UserActiveStatus.Online, status);
    }

    [Fact]
    public void GetUserActiveStatus_ShouldReturnRecentlyOnline_WhenUserLastSeenWithin24Hours()
    {
        // Arrange
        var service = new UserPresenceService();
        var userId = Guid.NewGuid().ToString();

        // Act
        var status = service.GetUserActiveStatus(userId, DateTime.UtcNow.AddHours(-5));

        // Assert
        Assert.Equal(BuildSmart.Core.Domain.Enums.UserActiveStatus.RecentlyOnline, status);
    }

    [Fact]
    public void GetUserActiveStatus_ShouldReturnOffline_WhenUserLastSeenOlderThan24Hours()
    {
        // Arrange
        var service = new UserPresenceService();
        var userId = Guid.NewGuid().ToString();

        // Act
        var status = service.GetUserActiveStatus(userId, DateTime.UtcNow.AddHours(-25));

        // Assert
        Assert.Equal(BuildSmart.Core.Domain.Enums.UserActiveStatus.Offline, status);
    }

    [Fact]
    public void GetUserActiveStatus_ShouldReturnOffline_WhenUserLastSeenIsNull()
    {
        // Arrange
        var service = new UserPresenceService();
        var userId = Guid.NewGuid().ToString();

        // Act
        var status = service.GetUserActiveStatus(userId, null);

        // Assert
        Assert.Equal(BuildSmart.Core.Domain.Enums.UserActiveStatus.Offline, status);
    }
}
