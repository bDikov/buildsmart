using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildSmart.SharedUI.GraphQL;
using BuildSmart.SharedUI.Services;
using BuildSmart.SharedUI.ViewModels;
using Moq;
using StrawberryShake;
using Xunit;

namespace BuildSmart.Maui.Tests
{
    public class FeedPageViewModelTests
    {
        private readonly Mock<IBuildSmartApiClient> _mockApiClient;
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly FeedPageViewModel _viewModel;

        public FeedPageViewModelTests()
        {
            _mockApiClient = new Mock<IBuildSmartApiClient>();
            _mockAuthService = new Mock<IAuthService>();
            
            // Mock main thread execution to run synchronously in tests
            AppServiceLocator.MainThread = new MockMainThread();
            AppServiceLocator.Navigation = new Mock<INavigationBridge>().Object;
            AppServiceLocator.Alerts = new Mock<IAlertService>().Object;

            _viewModel = new FeedPageViewModel(_mockApiClient.Object, _mockAuthService.Object);
        }

        [Fact]
        public void LoopNextVideoFromCache_WithEmptyCache_DoesNothing()
        {
            // Arrange
            Assert.Empty(_viewModel.FeedVideos);
            
            // Act
            _viewModel.LoopNextVideoFromCache();

            // Assert
            Assert.Empty(_viewModel.FeedVideos); // Should not crash
        }

        [Fact]
        public async Task LoadFeedMediaAsync_OnCacheHit_DoesNotHitApiAgain()
        {
            // Arrange
            _mockAuthService.Setup(a => a.GetTokenAsync()).ReturnsAsync("fake_token");
            _mockAuthService.Setup(a => a.GetUserRoleFromToken(It.IsAny<string>())).Returns("Homeowner");

            // Mock the MyProjects query to prevent errors
            var mockMyProjectsResult = new Mock<IOperationResult<IGetMyProjectsResult>>();
            var mockProjectsData = new Mock<IGetMyProjectsResult>();
            mockProjectsData.Setup(d => d.MyProjects).Returns(new List<IGetMyProjects_MyProjects>());
            mockMyProjectsResult.Setup(r => r.Data).Returns(mockProjectsData.Object);
            mockMyProjectsResult.Setup(r => r.Errors).Returns(new List<IClientError>());

            var mockGetMyProjectsQuery = new Mock<IGetMyProjectsQuery>();
            mockGetMyProjectsQuery.Setup(q => q.ExecuteAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockMyProjectsResult.Object);
            _mockApiClient.Setup(api => api.GetMyProjects).Returns(mockGetMyProjectsQuery.Object);

            // Mock the GetServiceCategories query
            var mockCategoriesResult = new Mock<IOperationResult<IGetServiceCategoriesResult>>();
            var mockCategoriesData = new Mock<IGetServiceCategoriesResult>();
            mockCategoriesData.Setup(d => d.ServiceCategories).Returns(new List<IGetServiceCategories_ServiceCategories>());
            mockCategoriesResult.Setup(r => r.Data).Returns(mockCategoriesData.Object);
            mockCategoriesResult.Setup(r => r.Errors).Returns(new List<IClientError>());
            
            var mockGetCategoriesQuery = new Mock<IGetServiceCategoriesQuery>();
            mockGetCategoriesQuery.Setup(q => q.ExecuteAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCategoriesResult.Object);
            _mockApiClient.Setup(api => api.GetServiceCategories).Returns(mockGetCategoriesQuery.Object);

            // Mock the GetFeedMedia query to return exactly 1 video
            var mockFeedResult = new Mock<IOperationResult<IGetFeedMediaResult>>();
            var mockFeedData = new Mock<IGetFeedMediaResult>();
            
            var mockFeedMediaConnection = new Mock<IGetFeedMedia_FeedMedia>();
            var mockItem = new Mock<IGetFeedMedia_FeedMedia_Items>();
            var mediaId = Guid.NewGuid().ToString(); // Ensure this matches the expected ID type (string)
            mockItem.Setup(i => i.Id).Returns(mediaId);
            mockItem.Setup(i => i.TradesmanId).Returns(Guid.NewGuid().ToString());
            mockItem.Setup(i => i.VideoUrl).Returns("test.mp4");
            
            var mockPageInfo = new Mock<IGetFeedMedia_FeedMedia_PageInfo>();
            mockPageInfo.Setup(p => p.HasNextPage).Returns(false);

            mockFeedMediaConnection.Setup(m => m.Items).Returns(new List<IGetFeedMedia_FeedMedia_Items> { mockItem.Object });
            mockFeedMediaConnection.Setup(m => m.PageInfo).Returns(mockPageInfo.Object);

            mockFeedData.Setup(d => d.FeedMedia).Returns(mockFeedMediaConnection.Object);
            mockFeedResult.Setup(r => r.Data).Returns(mockFeedData.Object);
            mockFeedResult.Setup(r => r.Errors).Returns(new List<IClientError>());

            var mockGetFeedMediaQuery = new Mock<IGetFeedMediaQuery>();
            mockGetFeedMediaQuery.Setup(q => q.ExecuteAsync(It.IsAny<TradesmanMediaFilterInput>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockFeedResult.Object);
            _mockApiClient.Setup(api => api.GetFeedMedia).Returns(mockGetFeedMediaQuery.Object);

            // Act 1: Initial Load
            await _viewModel.LoadFeedCommand.ExecuteAsync(null);

            // Assert 1
            Assert.Single(_viewModel.FeedVideos);
            
            // Act 2: Second Load (simulating navigating back to page)
            // If the cache shield works, it will NOT execute the query again
            await _viewModel.LoadFeedCommand.ExecuteAsync(null);

            // Assert 2: Verify the API was only called exactly ONE time during the first load
            mockGetFeedMediaQuery.Verify(q => q.ExecuteAsync(It.IsAny<TradesmanMediaFilterInput>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // Mock implementation to bypass MAUI MainThread dispatching in unit tests
        private class MockMainThread : IAppMainThread
        {
            public bool IsMainThread => true;
            public void BeginInvokeOnMainThread(Action action) => action();
            public Task InvokeOnMainThreadAsync(Action action) { action(); return Task.CompletedTask; }
            public Task<T> InvokeOnMainThreadAsync<T>(Func<T> func) => Task.FromResult(func());
            public Task InvokeOnMainThreadAsync(Func<Task> func) => func();
            public Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> func) => func();
        }
    }
}
