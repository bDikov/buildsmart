using Xunit;
using Moq;
using BuildSmart.SharedUI.ViewModels;
using BuildSmart.SharedUI.GraphQL;
using FluentAssertions;
using StrawberryShake;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace BuildSmart.Maui.Tests;

public class AuctionHubViewModelTests
{
    private readonly Mock<IBuildSmartApiClient> _mockApiClient;
    private readonly Mock<BuildSmart.SharedUI.Services.SignalRService> _mockSignalRService;
    private readonly AuctionHubViewModel _viewModel;

    public AuctionHubViewModelTests()
    {
        _mockApiClient = new Mock<IBuildSmartApiClient>();
        var authServiceMock = new Mock<BuildSmart.SharedUI.Services.IAuthService>();
        _mockSignalRService = new Mock<BuildSmart.SharedUI.Services.SignalRService>(authServiceMock.Object);
        _viewModel = new AuctionHubViewModel(_mockApiClient.Object, _mockSignalRService.Object);
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

        _mockApiClient.Setup(c => c.GetQuestionReplies).Returns(queryMock.Object);

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
    public async Task InitializeAsync_SetsHasSubmittedBid_WhenMatchingBidExists()
    {
        // Arrange
        var testJobId = Guid.NewGuid();
        var testProfileId = Guid.NewGuid().ToString();

        // 1. Mock Current User
        var mockUserResult = new Mock<IGetCurrentUserResult>();
        var mockCurrentUser = new Mock<IGetCurrentUser_CurrentUser>();
        var mockTradesmanProfile = new Mock<IGetCurrentUser_CurrentUser_TradesmanProfile>();
        
        mockTradesmanProfile.Setup(t => t.Id).Returns(testProfileId);
        mockCurrentUser.Setup(u => u.TradesmanProfile).Returns(mockTradesmanProfile.Object);
        mockUserResult.Setup(d => d.CurrentUser).Returns(mockCurrentUser.Object);

        var userResponse = new Mock<IOperationResult<IGetCurrentUserResult>>();
        userResponse.Setup(r => r.Data).Returns(mockUserResult.Object);
        userResponse.Setup(r => r.Errors).Returns(new List<IClientError>());

        var userQuery = new Mock<IGetCurrentUserQuery>();
        userQuery.Setup(q => q.ExecuteAsync(default)).ReturnsAsync(userResponse.Object);
        _mockApiClient.Setup(c => c.GetCurrentUser).Returns(userQuery.Object);

        // 2. Mock Auction Data with a Bid
        var mockAuctionResult = new Mock<IGetAuctionByIdResult>();
        var mockAuction = new Mock<IGetAuctionById_AuctionById>();
        
        var mockBid = new Mock<IGetAuctionById_AuctionById_Bids>();
        var mockBidProfile = new Mock<IGetAuctionById_AuctionById_Bids_TradesmanProfile>();
        mockBidProfile.Setup(t => t.Id).Returns(testProfileId);
        mockBid.Setup(b => b.TradesmanProfile).Returns(mockBidProfile.Object);

        mockAuction.Setup(a => a.Bids).Returns(new List<IGetAuctionById_AuctionById_Bids> { mockBid.Object });
        mockAuctionResult.Setup(d => d.AuctionById).Returns(mockAuction.Object);

        var auctionResponse = new Mock<IOperationResult<IGetAuctionByIdResult>>();
        auctionResponse.Setup(r => r.Data).Returns(mockAuctionResult.Object);
        auctionResponse.Setup(r => r.Errors).Returns(new List<IClientError>());

        var auctionQuery = new Mock<IGetAuctionByIdQuery>();
        auctionQuery.Setup(q => q.ExecuteAsync(testJobId, default)).ReturnsAsync(auctionResponse.Object);
        _mockApiClient.Setup(c => c.GetAuctionById).Returns(auctionQuery.Object);

        // 3. Mock Job Tasks
        var tasksResponse = new Mock<IOperationResult<IGetJobTasksResult>>();
        tasksResponse.Setup(r => r.Errors).Returns(new List<IClientError>());
        var tasksQuery = new Mock<IGetJobTasksQuery>();
        tasksQuery.Setup(q => q.ExecuteAsync(testJobId, default)).ReturnsAsync(tasksResponse.Object);
        _mockApiClient.Setup(c => c.GetJobTasks).Returns(tasksQuery.Object);

        _viewModel.JobId = testJobId.ToString();

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        _viewModel.HasSubmittedBid.Should().BeTrue();
        _viewModel.MyBid.Should().NotBeNull();
    }

    [Fact]
    public async Task ToggleConversationAsync_ExpandsAndLoadsReplies()
    {
        // Arrange
        var questionId = Guid.NewGuid();
        var mockQuestion = new Mock<IQuestionDetails>();
        mockQuestion.Setup(q => q.Id).Returns(questionId);
        mockQuestion.Setup(q => q.ReplyCount).Returns(5);
        
        var questionVm = new QuestionViewModel(mockQuestion.Object, async (vm) => await _viewModel.LoadMoreRepliesCommand.ExecuteAsync(vm));
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

        _mockApiClient.Setup(c => c.GetQuestionReplies).Returns(queryMock.Object);

        // Act
        await _viewModel.ToggleConversationCommand.ExecuteAsync(questionVm);

        // Assert
        questionVm.IsExpanded.Should().BeTrue();
        questionVm.Replies.Should().HaveCount(1);
    }
}
