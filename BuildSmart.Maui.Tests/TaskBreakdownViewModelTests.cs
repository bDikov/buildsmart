using BuildSmart.Maui.GraphQL;
using BuildSmart.Maui.ViewModels;
using Moq;
using StrawberryShake;
using System.Text.Json;
using Xunit;

namespace BuildSmart.Maui.Tests;

public class TaskBreakdownViewModelTests
{
    private readonly Mock<IBuildSmartApiClient> _mockApiClient;
    private readonly TaskBreakdownViewModel _viewModel;

    public TaskBreakdownViewModelTests()
    {
        _mockApiClient = new Mock<IBuildSmartApiClient>();
        _viewModel = new TaskBreakdownViewModel(_mockApiClient.Object);
    }

    [Fact]
    public async Task ApplyQueryAttributes_ValidJob_TriggersLazyLoad_AndPopulatesCollection()
    {
        // Arrange
        var testJobId = Guid.NewGuid();
        var mockJob = new Mock<IJobPostDetails>();
        mockJob.Setup(j => j.Id).Returns(testJobId);

        var query = new Dictionary<string, object>
        {
            { "Job", mockJob.Object }
        };

        // Create a mock task and mock job post to satisfy the nested StrawberryShake interface graph
        var mockTask = new Mock<IGetJobTasks_AllJobPosts_JobTasks>();
        mockTask.Setup(t => t.Title).Returns("Mocked DB Task");
        mockTask.Setup(t => t.SequenceOrder).Returns(1);

        var mockJobPost = new Mock<IGetJobTasks_AllJobPosts>();
        mockJobPost.Setup(j => j.JobTasks).Returns(new List<IGetJobTasks_AllJobPosts_JobTasks> { mockTask.Object });

        var mockResult = new Mock<IGetJobTasksResult>();
        mockResult.Setup(r => r.AllJobPosts).Returns(new List<IGetJobTasks_AllJobPosts> { mockJobPost.Object });

        var mockResponse = new Mock<IOperationResult<IGetJobTasksResult>>();
        mockResponse.Setup(r => r.Data).Returns(mockResult.Object);

        _mockApiClient.Setup(c => c.GetJobTasks.ExecuteAsync(testJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse.Object);

        // Act
        _viewModel.ApplyQueryAttributes(query);
        
        // Wait a tiny bit since ApplyQueryAttributes is async void (fire and forget)
        await Task.Delay(100);

        // Assert
        Assert.Equal(mockJob.Object, _viewModel.Job);
        _mockApiClient.Verify(c => c.GetJobTasks.ExecuteAsync(testJobId, It.IsAny<CancellationToken>()), Times.Once);
        
        Assert.Single(_viewModel.Tasks);
        Assert.Equal("Mocked DB Task", _viewModel.Tasks[0].Title);
        Assert.False(_viewModel.IsLoading);
    }
}
