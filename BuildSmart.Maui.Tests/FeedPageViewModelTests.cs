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
        public async Task LoadFeedAsync_AsHomeowner_PopulatesFeedVideosWithActiveMedia()
        {
            // Arrange
            _mockAuthService.Setup(a => a.GetTokenAsync()).ReturnsAsync("fake_token");
            _mockAuthService.Setup(a => a.GetUserRoleFromToken(It.IsAny<string>())).Returns("Homeowner");

            var mockMyProjectsResult = new Mock<IOperationResult<IGetMyProjectsResult>>();
            var mockProjectsData = new Mock<IGetMyProjectsResult>();
            mockProjectsData.Setup(d => d.MyProjects).Returns(new List<IGetMyProjects_MyProjects>());
            mockMyProjectsResult.Setup(r => r.Data).Returns(mockProjectsData.Object);
            mockMyProjectsResult.Setup(r => r.Errors).Returns(new List<IClientError>());

            var mockGetMyProjectsQuery = new Mock<IGetMyProjectsQuery>();
            mockGetMyProjectsQuery.Setup(q => q.ExecuteAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockMyProjectsResult.Object);
            _mockApiClient.Setup(api => api.GetMyProjects).Returns(mockGetMyProjectsQuery.Object);

            var mockTradesmanProfilesResult = new Mock<IOperationResult<IGetTradesmanProfilesResult>>();
            var mockProfilesData = new Mock<IGetTradesmanProfilesResult>();
            
            var tradesmanId = Guid.NewGuid().ToString(); // Fix Guid to String
            var activeMedia = new Mock<IGetTradesmanProfiles_TradesmanProfiles_Media>();
            activeMedia.Setup(m => m.Id).Returns(Guid.NewGuid().ToString()); // Fix Guid to String
            activeMedia.Setup(m => m.IsActive).Returns(true);
            activeMedia.Setup(m => m.VideoUrl).Returns("https://cdn.example.com/video1.mp4");

            var inactiveMedia = new Mock<IGetTradesmanProfiles_TradesmanProfiles_Media>();
            inactiveMedia.Setup(m => m.IsActive).Returns(false);

            var mockUser = new Mock<IGetTradesmanProfiles_TradesmanProfiles_User>();
            mockUser.Setup(u => u.FirstName).Returns("John");
            mockUser.Setup(u => u.LastName).Returns("Doe");
            mockUser.Setup(u => u.Location).Returns("Sofia");

            var mockProfile = new Mock<IGetTradesmanProfiles_TradesmanProfiles>();
            mockProfile.Setup(p => p.Id).Returns(tradesmanId);
            mockProfile.Setup(p => p.User).Returns(mockUser.Object);
            mockProfile.Setup(p => p.AverageRating).Returns(4.5);
            mockProfile.Setup(p => p.Media).Returns(new List<IGetTradesmanProfiles_TradesmanProfiles_Media> { activeMedia.Object, inactiveMedia.Object });

            mockProfilesData.Setup(d => d.TradesmanProfiles).Returns(new List<IGetTradesmanProfiles_TradesmanProfiles> { mockProfile.Object });
            mockTradesmanProfilesResult.Setup(r => r.Data).Returns(mockProfilesData.Object);
            mockTradesmanProfilesResult.Setup(r => r.Errors).Returns(new List<IClientError>());

            var mockGetProfilesQuery = new Mock<IGetTradesmanProfilesQuery>();
            mockGetProfilesQuery.Setup(q => q.ExecuteAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockTradesmanProfilesResult.Object);
            _mockApiClient.Setup(api => api.GetTradesmanProfiles).Returns(mockGetProfilesQuery.Object);

            // Act
            await _viewModel.LoadFeedCommand.ExecuteAsync(null);

            // Assert
            Assert.True(_viewModel.IsHomeowner);
            Assert.Single(_viewModel.FeedVideos); // Should only load the 1 active media, ignoring the inactive one

            var videoItem = _viewModel.FeedVideos.First();
            Assert.Equal(tradesmanId, videoItem.TradesmanId);
            Assert.Equal("https://cdn.example.com/video1.mp4", videoItem.VideoUrl);
            Assert.Equal("John Doe", videoItem.Name);
            Assert.Equal("Sofia", videoItem.Location);
            Assert.Equal(4.5, videoItem.Rating);
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
