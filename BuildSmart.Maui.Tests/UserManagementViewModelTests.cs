using Xunit;
using Moq;
using BuildSmart.Maui.ViewModels.Admin;
using BuildSmart.Maui.GraphQL;
using FluentAssertions;
using StrawberryShake;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace BuildSmart.Maui.Tests;

public class UserManagementViewModelTests
{
    private readonly Mock<IBuildSmartApiClient> _apiClientMock;
    private readonly UserManagementViewModel _viewModel;

    public UserManagementViewModelTests()
    {
        _apiClientMock = new Mock<IBuildSmartApiClient>();
        _viewModel = new UserManagementViewModel(_apiClientMock.Object);
    }

    [Fact]
    public async Task LoadUsersAsync_WhenSuccessful_PopulatesUsers()
    {
        // Arrange
        var users = new List<IGetUsers_Users>
        {
            new GetUsers_Users_User(Guid.NewGuid(), "John", "Doe", "john@doe.com", UserRoleTypes.Homeowner, null)
        };

        var responseMock = new Mock<IExecuteResult<IGetUsers>>();
        responseMock.Setup(r => r.Data).Returns(new GetUsers_Users(users));
        responseMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        var queryMock = new Mock<IGetUsersQuery>();
        queryMock.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(responseMock.Object);

        _apiClientMock.Setup(c => c.GetUsers).Returns(queryMock.Object);

        // Act
        await _viewModel.LoadUsersAsync();

        // Assert
        _viewModel.Users.Should().HaveCount(1);
        _viewModel.Users[0].FirstName.Should().Be("John");
        _viewModel.IsEmpty.Should().BeFalse();
    }

    // Concrete implementation for Mocking StrawberryShake interfaces
    private class GetUsers_Users : IGetUsers
    {
        public GetUsers_Users(IReadOnlyList<IGetUsers_Users> users)
        {
            Users = users;
        }
        public IReadOnlyList<IGetUsers_Users> Users { get; }
    }

    private class GetUsers_Users_User : IGetUsers_Users
    {
        public GetUsers_Users_User(Guid id, string firstName, string lastName, string email, UserRoleTypes role, IGetUsers_Users_TradesmanProfile? tradesmanProfile)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            Role = role;
            TradesmanProfile = tradesmanProfile;
        }

        public Guid Id { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string Email { get; }
        public UserRoleTypes Role { get; }
        public IGetUsers_Users_TradesmanProfile? TradesmanProfile { get; }
    }
}
