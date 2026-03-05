using Xunit;
using Moq;
using BuildSmart.Maui.ViewModels.Admin;
using BuildSmart.Maui.GraphQL;
using FluentAssertions;
using StrawberryShake;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

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
        var mockUser = new Mock<IGetUsers_Users>();
        mockUser.Setup(u => u.Id).Returns(Guid.NewGuid());
        mockUser.Setup(u => u.FirstName).Returns("John");
        mockUser.Setup(u => u.LastName).Returns("Doe");
        mockUser.Setup(u => u.Email).Returns("john@doe.com");
        mockUser.Setup(u => u.Role).Returns(UserRoleTypes.Homeowner);

        var resultDataMock = new Mock<IGetUsersResult>();
        resultDataMock.Setup(d => d.Users).Returns(new List<IGetUsers_Users> { mockUser.Object });

        var responseMock = new Mock<IOperationResult<IGetUsersResult>>();
        responseMock.Setup(r => r.Data).Returns(resultDataMock.Object);
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
}
