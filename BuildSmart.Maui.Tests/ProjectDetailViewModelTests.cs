using Xunit;
using Moq;
using BuildSmart.Maui.ViewModels;
using BuildSmart.Maui.GraphQL;
using BuildSmart.Maui.Services;
using FluentAssertions;
using StrawberryShake;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace BuildSmart.Maui.Tests;

public class ProjectDetailViewModelTests
{
    private readonly Mock<IBuildSmartApiClient> _apiClientMock;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly SignalRService _signalRService;
    private readonly ProjectDetailViewModel _viewModel;

    public ProjectDetailViewModelTests()
    {
        _apiClientMock = new Mock<IBuildSmartApiClient>();
        _authServiceMock = new Mock<IAuthService>();
        _signalRService = new SignalRService(_authServiceMock.Object);
        _viewModel = new ProjectDetailViewModel(_apiClientMock.Object, _signalRService, _authServiceMock.Object);
    }

    [Fact]
    public void ApplyQueryAttributes_SetsProject()
    {
        // Arrange
        var projectMock = new Mock<IGetMyProjects_MyProjects>();
        var query = new Dictionary<string, object>
        {
            { "Project", projectMock.Object }
        };

        // Act
        _viewModel.ApplyQueryAttributes(query);

        // Assert
        _viewModel.Project.Should().Be(projectMock.Object);
    }

    [Fact]
    public async Task ReloadProjectAsync_UpdatesProject()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var initialProject = new MockProject { Id = projectId, Title = "Initial" };
        _viewModel.Project = initialProject;

        var updatedProject = new MockProject { Id = projectId, Title = "Updated" };
        var projectsList = new List<IGetMyProjects_MyProjects> { updatedProject };

        var responseMock = new Mock<IExecuteResult<IGetMyProjects>>();
        responseMock.Setup(r => r.Data).Returns(new MockGetMyProjects(projectsList));
        responseMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        var queryMock = new Mock<IGetMyProjectsQuery>();
        queryMock.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(responseMock.Object);

        _apiClientMock.Setup(c => c.GetMyProjects).Returns(queryMock.Object);

        // Act
        // ReloadProjectAsync is private, but called by OnNotificationReceived or we can use reflection if needed.
        // For this test, we want to verify it works when called.
        // Since we can't call it directly easily, let's trigger it via notification if possible or 
        // just test the logic if we make it internal/public for testing.
        // Given it's private, I'll use reflection for a surgical test of the reload logic.
        var method = typeof(ProjectDetailViewModel).GetMethod("ReloadProjectAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(_viewModel, null);

        // Assert
        _viewModel.Project.Should().NotBeNull();
        _viewModel.Project.Title.Should().Be("Updated");
    }

    // Mock classes for StrawberryShake
    private class MockGetMyProjects : IGetMyProjects
    {
        public MockGetMyProjects(IReadOnlyList<IGetMyProjects_MyProjects> myProjects)
        {
            MyProjects = myProjects;
        }
        public IReadOnlyList<IGetMyProjects_MyProjects> MyProjects { get; }
    }

    private class MockProject : IGetMyProjects_MyProjects
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public ProjectStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public IReadOnlyList<IGetMyProjects_MyProjects_JobPosts> JobPosts { get; set; } = new List<IGetMyProjects_MyProjects_JobPosts>();
    }
}
