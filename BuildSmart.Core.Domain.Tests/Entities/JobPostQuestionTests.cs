using BuildSmart.Core.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace BuildSmart.Core.Domain.Tests.Entities;

public class JobPostQuestionTests
{
    [Fact]
    public void Can_Create_Nested_Reply()
    {
        // Arrange
        var parentQuestion = new JobPostQuestion
        {
            Id = Guid.NewGuid(),
            QuestionText = "Is this wall load bearing?"
        };

        // Act
        var reply = new JobPostQuestion
        {
            Id = Guid.NewGuid(),
            QuestionText = "I checked the blueprints, it is.",
            ParentQuestionId = parentQuestion.Id,
            ParentQuestion = parentQuestion
        };
        
        parentQuestion.Replies.Add(reply);

        // Assert
        reply.ParentQuestionId.Should().Be(parentQuestion.Id);
        reply.ParentQuestion.Should().Be(parentQuestion);
        parentQuestion.Replies.Should().ContainSingle();
        parentQuestion.Replies.First().Should().Be(reply);
    }
}
