using System;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BuildSmart.Core.Domain.Tests.Entities;

public class JobPostTests
{
    [Fact]
    public void SetGeneratedScope_ShouldKeepStatusAsGeneratingScope()
    {
        // Arrange
        var jobPost = new JobPost();
        jobPost.SubmitForScopeGeneration(); // Status -> GeneratingScope

        // Act
        jobPost.SetGeneratedScope("AI generated scope markdown");

        // Assert
        jobPost.Status.Should().Be(JobPostStatus.GeneratingScope);
        jobPost.GeneratedScope.Should().Be("AI generated scope markdown");
    }

    [Fact]
    public void CompletePricing_ShouldTransitionStatusToWaitingForUserReview_WhenStatusIsGeneratingScope()
    {
        // Arrange
        var jobPost = new JobPost();
        jobPost.SubmitForScopeGeneration(); // Status -> GeneratingScope
        jobPost.SetGeneratedScope("AI generated scope markdown");

        // Act
        jobPost.CompletePricing();

        // Assert
        jobPost.Status.Should().Be(JobPostStatus.WaitingForUserReview);
    }

    [Fact]
    public void CompletePricing_ShouldDoNothing_WhenStatusIsNotGeneratingScope()
    {
        // Arrange
        var jobPost = new JobPost(); // Status -> Draft

        // Act
        jobPost.CompletePricing();

        // Assert
        jobPost.Status.Should().Be(JobPostStatus.Draft);
    }

    [Fact]
    public void ApproveScope_ShouldTransitionToWaitingForAdminReview_WhenStatusIsWaitingForUserReview()
    {
        // Arrange
        var jobPost = new JobPost();
        jobPost.SubmitForScopeGeneration(); // Status -> GeneratingScope
        jobPost.SetGeneratedScope("AI generated scope markdown");
        jobPost.CompletePricing(); // Status -> WaitingForUserReview

        // Act
        jobPost.ApproveScope("Final approved scope markdown");

        // Assert
        jobPost.Status.Should().Be(JobPostStatus.WaitingForAdminReview);
        jobPost.UserEditedScope.Should().Be("Final approved scope markdown");
        jobPost.Description.Should().Be("Final approved scope markdown");
    }

    [Fact]
    public void ApproveScope_ShouldThrow_WhenStatusIsNotWaitingForUserReview()
    {
        // Arrange
        var jobPost = new JobPost(); // Status -> Draft

        // Act
        Action act = () => jobPost.ApproveScope("Scope text");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Job is not waiting for user review. Current Status: Draft");
    }
}
