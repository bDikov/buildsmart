using BuildSmart.Maui.GraphQL;
using BuildSmart.Maui.ViewModels;
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
        var mockJob = new Mock<IJobPostDetails>();
        mockJob.Setup(j => j.Id).Returns(Guid.NewGuid());
        mockJob.Setup(j => j.GeneratedScope).Returns("Generated Scope Data");

        var query = new Dictionary<string, object>
        {
            { "Job", mockJob.Object }
        };

        // Act
        _viewModel.ApplyQueryAttributes(query);

        // Assert
        Assert.Equal("Generated Scope Data", _viewModel.GeneratedScope);
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
}
