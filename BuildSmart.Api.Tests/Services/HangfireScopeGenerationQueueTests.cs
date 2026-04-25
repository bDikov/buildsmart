using BuildSmart.Api.Services;
using BuildSmart.Api.Workers;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Moq;

namespace BuildSmart.Api.Tests.Services;

public class HangfireScopeGenerationQueueTests
{
	[Fact]
	public async Task QueueBackgroundWorkItemAsync_ShouldEnqueueJob()
	{
		// Arrange
		var mockBackgroundJobClient = new Mock<IBackgroundJobClient>();
		var queue = new HangfireScopeGenerationQueue(mockBackgroundJobClient.Object);
		var jobId = Guid.NewGuid();

		// Act
		await queue.QueueBackgroundWorkItemAsync(jobId, CancellationToken.None);

		// Assert
		mockBackgroundJobClient.Verify(x => x.Create(
			It.Is<Job>(job => job.Method.Name == nameof(ScopeGenerationWorker.ProcessJobAsync) && (Guid)job.Args[0] == jobId),
			It.IsAny<EnqueuedState>()), Times.Once);
	}

	[Fact]
	public async Task DequeueAsync_ShouldThrowNotSupportedException()
	{
		// Arrange
		var mockBackgroundJobClient = new Mock<IBackgroundJobClient>();
		var queue = new HangfireScopeGenerationQueue(mockBackgroundJobClient.Object);

		// Act & Assert
		await Assert.ThrowsAsync<NotSupportedException>(async () => await queue.DequeueAsync(CancellationToken.None));
	}
}