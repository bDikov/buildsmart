using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using Xunit;
using TaskStatus = BuildSmart.Core.Domain.Enums.TaskStatus;

namespace BuildSmart.Core.Domain.Tests.Entities;

public class JobTaskKanbanTests
{
    [Fact]
    public void StartWork_ShouldTransitionFromToDoToInProgress()
    {
        var task = new JobTask
        {
            Id = Guid.NewGuid(),
            Title = "Install Tiles",
            Status = TaskStatus.ToDo
        };

        task.StartWork();

        Assert.Equal(TaskStatus.InProgress, task.Status);
    }

    [Fact]
    public void SubmitForApproval_ShouldTransitionFromInProgressToAwaitingApproval()
    {
        var task = new JobTask
        {
            Id = Guid.NewGuid(),
            Title = "Install Tiles",
            Status = TaskStatus.InProgress
        };

        task.SubmitForApproval();

        Assert.Equal(TaskStatus.AwaitingApproval, task.Status);
    }

    [Fact]
    public void Approve_ShouldTransitionToDoneAndInitializePaymentRecord()
    {
        var task = new JobTask
        {
            Id = Guid.NewGuid(),
            Title = "Install Tiles",
            Status = TaskStatus.AwaitingApproval,
            EstimatedPrice = 150.50m
        };

        task.Approve();

        Assert.Equal(TaskStatus.Done, task.Status);
        Assert.NotNull(task.PaymentRecord);
        Assert.Equal(PaymentStatus.AwaitingPayment, task.PaymentRecord.Status);
        Assert.Equal(150.50m, task.PaymentRecord.CalculatedAmount);
    }

    [Fact]
    public void Reject_ShouldRevertToInProgressAndAppendRejectionComment()
    {
        var task = new JobTask
        {
            Id = Guid.NewGuid(),
            Title = "Install Tiles",
            Status = TaskStatus.AwaitingApproval
        };
        var authorId = Guid.NewGuid();

        task.Reject(authorId, "Grout line uneven on east wall.");

        Assert.Equal(TaskStatus.InProgress, task.Status);
        Assert.Single(task.Comments);
        var comment = task.Comments.First();
        Assert.True(comment.IsSystemNote);
        Assert.Contains("Grout line uneven on east wall.", comment.Content);
    }
}
