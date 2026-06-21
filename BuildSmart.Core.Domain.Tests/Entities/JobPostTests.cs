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
}
