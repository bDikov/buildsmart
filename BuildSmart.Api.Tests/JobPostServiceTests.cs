using System;
using System.Threading.Tasks;
using BuildSmart.Core.Application.Services;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Moq;
using Xunit;
using FluentAssertions;
using BuildSmart.Core.Domain.Enums;

namespace BuildSmart.Api.Tests;

public class JobPostServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IScopeGenerationQueue> _mockQueue;
    private readonly Mock<INotificationService> _mockNotification;
    private readonly Mock<IJobsNotificationService> _mockJobsNotification;
    private readonly JobPostService _service;

    public JobPostServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockQueue = new Mock<IScopeGenerationQueue>();
        _mockNotification = new Mock<INotificationService>();
        _mockJobsNotification = new Mock<IJobsNotificationService>();

        _service = new JobPostService(
            _mockUow.Object,
            _mockQueue.Object,
            _mockNotification.Object,
            _mockJobsNotification.Object
        );
    }

    [Fact]
    public async Task ReplyToQuestionAsync_ShouldCreateReplyAndNotify()
    {
        // Arrange
        var parentQuestionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var replyText = "This is a reply";
        var jobPostId = Guid.NewGuid();
        
        var parentQuestion = new JobPostQuestion
        {
            Id = parentQuestionId,
            JobPostId = jobPostId,
            AuthorId = Guid.NewGuid(),
            QuestionText = "Original question"
        };

        var user = new User
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        };

        _mockUow.Setup(u => u.JobPostQuestions.GetByIdAsync(parentQuestionId))
            .ReturnsAsync(parentQuestion);
            
        _mockUow.Setup(u => u.Users.GetByIdAsync(userId))
            .ReturnsAsync(user);

        JobPostQuestion? capturedReply = null;
        _mockUow.Setup(u => u.JobPostQuestions.AddAsync(It.IsAny<JobPostQuestion>()))
            .Callback<JobPostQuestion>(q => capturedReply = q)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ReplyToQuestionAsync(parentQuestionId, userId, replyText);

        // Assert
        result.Should().NotBeNull();
        result.ParentQuestionId.Should().Be(parentQuestionId);
        result.AuthorId.Should().Be(userId);
        result.QuestionText.Should().Be(replyText);
        
        capturedReply.Should().NotBeNull();
        capturedReply!.ParentQuestionId.Should().Be(parentQuestionId);

        _mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        _mockNotification.Verify(n => n.SendNotificationAsync(
            parentQuestion.AuthorId.Value,
            "New Reply",
            It.IsAny<string>(),
            It.IsAny<Guid?>(),
            It.IsAny<string>(),
            It.IsAny<object>()
        ), Times.Once);
    }
}
