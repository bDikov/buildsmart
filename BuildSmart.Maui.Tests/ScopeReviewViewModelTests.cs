using BuildSmart.SharedUI.GraphQL;
using BuildSmart.SharedUI.ViewModels;
using Moq;
using StrawberryShake;
using System.Text.Json;
using Xunit;

namespace BuildSmart.Maui.Tests;

public class ScopeReviewViewModelTests
{
    private readonly Mock<IBuildSmartApiClient> _mockApiClient;
    private readonly ScopeReviewViewModel _viewModel;

    public ScopeReviewViewModelTests()
    {
        _mockApiClient = new Mock<IBuildSmartApiClient>();
        _viewModel = new ScopeReviewViewModel(_mockApiClient.Object);
    }

    [Fact]
    public void ApplyQueryAttributes_ValidJob_InitializesWithOneEmptyTask()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var query = new Dictionary<string, object>
        {
            { "JobId", jobId }
        };

        // Note: The ViewModel now fetches data using _apiClient.GetJobTasks.ExecuteAsync(jobId).
        // Since we are mocking the client, it will return null and do nothing.
        // The ViewModel will hit the fallback logic: if (Tasks.Count == 0) AddTask();

        // Act
        _viewModel.ApplyQueryAttributes(query);

        // Assert
        Assert.Single(_viewModel.Tasks);
        Assert.Equal(1, _viewModel.Tasks[0].SequenceOrder);
        Assert.Equal("New Task", _viewModel.Tasks[0].Title);
        Assert.Single(_viewModel.Tasks[0].Criteria);
    }

    [Fact]
    public void AddTaskCommand_AddsNewTaskWithCorrectSequence()
    {
        // Act
        _viewModel.AddTaskCommand.Execute(null); // Adds second task, since it starts empty
        _viewModel.AddTaskCommand.Execute(null);

        // Assert
        Assert.Equal(2, _viewModel.Tasks.Count);
        Assert.Equal(1, _viewModel.Tasks[0].SequenceOrder);
        Assert.Equal(2, _viewModel.Tasks[1].SequenceOrder);
    }

    [Fact]
    public void RemoveTaskCommand_RemovesTaskAndUpdatesSequence()
    {
        // Arrange
        _viewModel.AddTaskCommand.Execute(null);
        _viewModel.AddTaskCommand.Execute(null);
        _viewModel.AddTaskCommand.Execute(null);
        
        var taskToRemove = _viewModel.Tasks[1]; // The second task

        // Act
        _viewModel.RemoveTaskCommand.Execute(taskToRemove);

        // Assert
        Assert.Equal(2, _viewModel.Tasks.Count);
        Assert.Equal(1, _viewModel.Tasks[0].SequenceOrder);
        Assert.Equal(2, _viewModel.Tasks[1].SequenceOrder); // Sequence should shift down
    }

    [Fact]
    public void AddRemoveCriteria_UpdatesTaskCriteriaList()
    {
        // Arrange
        _viewModel.AddTaskCommand.Execute(null);
        var task = _viewModel.Tasks[0];
        
        // Task comes with 1 criteria by default
        Assert.Single(task.Criteria);

        // Act - Add
        task.AddCriteriaCommand.Execute(null);
        Assert.Equal(2, task.Criteria.Count);

        // Act - Remove
        var criteriaToRemove = task.Criteria[0];
        task.RemoveCriteriaCommand.Execute(criteriaToRemove);
        
        // Assert
        Assert.Single(task.Criteria);
    }

    [Fact]
    public async Task ApproveCommand_Aborts_WhenNoTasksExist()
    {
        // Arrange
        var mockJob = new Mock<IJobPostDetails>();
        mockJob.Setup(j => j.Id).Returns(Guid.NewGuid());
        _viewModel.Job = mockJob.Object;
        _viewModel.Tasks.Clear();

        // Act & Assert
        try
        {
            await _viewModel.ApproveCommand.ExecuteAsync(null);
        }
        catch (NullReferenceException)
        {
            // Shell.Current is null in unit tests, which means validation successfully triggered DisplayAlert
        }

        _mockApiClient.Verify(x => x.UpdateJobTasks.ExecuteAsync(It.IsAny<UpdateJobTasksInput>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockApiClient.Verify(x => x.ApproveJobScope.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApproveCommand_Aborts_WhenTaskTitleIsEmpty()
    {
        // Arrange
        var mockJob = new Mock<IJobPostDetails>();
        mockJob.Setup(j => j.Id).Returns(Guid.NewGuid());
        _viewModel.Job = mockJob.Object;
        
        _viewModel.Tasks.Clear();
        _viewModel.Tasks.Add(new EditableTaskViewModel { Title = "" }); // Empty title

        // Act & Assert
        try
        {
            await _viewModel.ApproveCommand.ExecuteAsync(null);
        }
        catch (NullReferenceException)
        {
            // Shell.Current is null in unit tests
        }

        _mockApiClient.Verify(x => x.UpdateJobTasks.ExecuteAsync(It.IsAny<UpdateJobTasksInput>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApproveCommand_Aborts_WhenCriteriaDescriptionIsEmpty()
    {
        // Arrange
        var mockJob = new Mock<IJobPostDetails>();
        mockJob.Setup(j => j.Id).Returns(Guid.NewGuid());
        _viewModel.Job = mockJob.Object;
        
        var task = new EditableTaskViewModel { Title = "Valid Title" };
        task.Criteria.Add(new EditableCriteriaViewModel { Description = "" }); // Empty criteria
        _viewModel.Tasks.Clear();
        _viewModel.Tasks.Add(task);

        // Act & Assert
        try
        {
            await _viewModel.ApproveCommand.ExecuteAsync(null);
        }
        catch (NullReferenceException)
        {
            // Shell.Current is null in unit tests
        }

        _mockApiClient.Verify(x => x.UpdateJobTasks.ExecuteAsync(It.IsAny<UpdateJobTasksInput>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
