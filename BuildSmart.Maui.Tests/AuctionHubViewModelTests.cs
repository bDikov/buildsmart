using Xunit;
using Moq;
using BuildSmart.Maui.ViewModels;
using BuildSmart.Maui.GraphQL;
using FluentAssertions;
using StrawberryShake;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace BuildSmart.Maui.Tests;

public class AuctionHubViewModelTests
{
    private readonly Mock<IBuildSmartApiClient> _apiClientMock;
    private readonly AuctionHubViewModel _viewModel;

    public AuctionHubViewModelTests()
    {
        _apiClientMock = new Mock<IBuildSmartApiClient>();
        _viewModel = new AuctionHubViewModel(_apiClientMock.Object);
    }

    [Fact]
    public async Task LoadMoreRepliesAsync_AppendsNewReplies()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var mockQuestion = new Mock<IQuestionDetails>();
        mockQuestion.Setup(q => q.Id).Returns(questionId);
        mockQuestion.Setup(q => q.ReplyCount).Returns(10);
        
        var questionVm = new QuestionViewModel(mockQuestion.Object);
        questionVm.Replies.Count.Should().Be(0);

        // CRITICAL: Mock the specific interface generated for this query
        var mockReply = new Mock<IGetQuestionReplies_QuestionReplies_Replies>();
        mockReply.Setup(r => r.Id).Returns(Guid.NewGuid());
        mockReply.Setup(r => r.QuestionText).Returns("Reply 1");

        var resultDataMock = new Mock<IGetQuestionRepliesResult>();
        var questionRepliesMock = new Mock<IGetQuestionReplies_QuestionReplies>();
        questionRepliesMock.Setup(q => q.Id).Returns(questionId);
        
        var repliesList = new List<IGetQuestionReplies_QuestionReplies_Replies> { mockReply.Object };
        questionRepliesMock.Setup(q => q.Replies).Returns(repliesList);
        
        resultDataMock.Setup(d => d.QuestionReplies).Returns(questionRepliesMock.Object);

        var responseMock = new Mock<IOperationResult<IGetQuestionRepliesResult>>();
        responseMock.Setup(r => r.Data).Returns(resultDataMock.Object);
        responseMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        var queryMock = new Mock<IGetQuestionRepliesQuery>();
        queryMock.Setup(q => q.ExecuteAsync(questionId, 0, 5, default)).ReturnsAsync(responseMock.Object);

        _apiClientMock.Setup(c => c.GetQuestionReplies).Returns(queryMock.Object);

        // Act
        questionVm.IsExpanded = true;
        await _viewModel.LoadMoreRepliesCommand.ExecuteAsync(questionVm);

        // Assert
        questionVm.Replies.Should().HaveCount(1);
        questionVm.Replies[0].QuestionText.Should().Be("Reply 1");
        questionVm.HasMoreReplies.Should().BeTrue();
        questionVm.ButtonText.Should().Contain("9 left");
    }

    [Fact]
    public async Task ToggleConversationAsync_ExpandsAndLoadsReplies()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var mockQuestion = new Mock<IQuestionDetails>();
        mockQuestion.Setup(q => q.Id).Returns(questionId);
        mockQuestion.Setup(q => q.ReplyCount).Returns(5);
        
        var questionVm = new QuestionViewModel(mockQuestion.Object);
        questionVm.IsExpanded.Should().BeFalse();

        var mockReply = new Mock<IGetQuestionReplies_QuestionReplies_Replies>();
        mockReply.Setup(r => r.Id).Returns(Guid.NewGuid());

        var resultDataMock = new Mock<IGetQuestionRepliesResult>();
        var questionRepliesMock = new Mock<IGetQuestionReplies_QuestionReplies>();
        questionRepliesMock.Setup(q => q.Replies).Returns(new List<IGetQuestionReplies_QuestionReplies_Replies> { mockReply.Object });
        resultDataMock.Setup(d => d.QuestionReplies).Returns(questionRepliesMock.Object);

        var responseMock = new Mock<IOperationResult<IGetQuestionRepliesResult>>();
        responseMock.Setup(r => r.Data).Returns(resultDataMock.Object);
        responseMock.Setup(r => r.Errors).Returns(new List<IClientError>());

        var queryMock = new Mock<IGetQuestionRepliesQuery>();
        queryMock.Setup(q => q.ExecuteAsync(questionId, 0, 5, default)).ReturnsAsync(responseMock.Object);

        _apiClientMock.Setup(c => c.GetQuestionReplies).Returns(queryMock.Object);

        // Act
        await _viewModel.ToggleConversationCommand.ExecuteAsync(questionVm);

        // Assert
        questionVm.IsExpanded.Should().BeTrue();
        questionVm.Replies.Should().HaveCount(1);
    }
}
