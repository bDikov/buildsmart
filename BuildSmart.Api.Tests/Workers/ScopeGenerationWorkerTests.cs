using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuildSmart.Api.Workers;
using BuildSmart.Core.Application.DTOs;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace BuildSmart.Api.Tests.Workers;

public class ScopeGenerationWorkerTests
{
	private readonly Mock<IServiceProvider> _mockServiceProvider;
	private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
	private readonly Mock<IServiceScope> _mockServiceScope;
	private readonly Mock<IUnitOfWork> _mockUnitOfWork;
	private readonly Mock<IAiService> _mockAiService;
	private readonly Mock<INotificationService> _mockNotificationService;
	private readonly Mock<ILogger<ScopeGenerationWorker>> _mockLogger;

	public ScopeGenerationWorkerTests()
	{
		_mockServiceProvider = new Mock<IServiceProvider>();
		_mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
		_mockServiceScope = new Mock<IServiceScope>();
		_mockUnitOfWork = new Mock<IUnitOfWork>();
		_mockAiService = new Mock<IAiService>();
		_mockNotificationService = new Mock<INotificationService>();
		_mockLogger = new Mock<ILogger<ScopeGenerationWorker>>();

		_mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
			.Returns(_mockServiceScopeFactory.Object);
		_mockServiceScopeFactory.Setup(f => f.CreateScope())
			.Returns(_mockServiceScope.Object);
		_mockServiceScope.Setup(s => s.ServiceProvider)
			.Returns(_mockServiceProvider.Object);

		_mockServiceProvider.Setup(sp => sp.GetService(typeof(IUnitOfWork)))
			.Returns(_mockUnitOfWork.Object);
		_mockServiceProvider.Setup(sp => sp.GetService(typeof(IAiService)))
			.Returns(_mockAiService.Object);
		_mockServiceProvider.Setup(sp => sp.GetService(typeof(INotificationService)))
			.Returns(_mockNotificationService.Object);
	}

	[Fact]
	public async Task ProcessJobAsync_ShouldNotDelay_OnFirstCall()
	{
		// Arrange
		var jobId = Guid.NewGuid();
		var jobPost = new JobPost { Id = jobId, ServiceCategoryId = Guid.NewGuid() };

		_mockUnitOfWork.Setup(u => u.JobPosts.GetByIdAsync(jobId))
			.ReturnsAsync(jobPost);
		_mockUnitOfWork.Setup(u => u.ServiceCategories.GetAllAsync())
			.ReturnsAsync(new List<ServiceCategory>());
		_mockUnitOfWork.Setup(u => u.ServiceSkus.GetByCategoryAsync(It.IsAny<Guid>()))
			.ReturnsAsync(new List<ServiceSku>());
		_mockUnitOfWork.Setup(u => u.JobTasks.GetTasksByJobPostAsync(jobId))
			.ReturnsAsync(new List<JobTask>());

		_mockAiService.Setup(a => a.GenerateJobScopeAsync(It.IsAny<JobPost>(), It.IsAny<string>(), It.IsAny<List<ServiceSku>>()))
			.ReturnsAsync(new AiScopeBreakdownResponse("Scope", new List<AiTaskBreakdownItem>()));

		// Reset last call time and mock delay/time
		ScopeGenerationWorker._lastApiCallTime = DateTime.MinValue;

		int delayCallCount = 0;
		ScopeGenerationWorker.DelayTask = (ts) =>
		{
			delayCallCount++;
			return Task.CompletedTask;
		};

		var worker = new ScopeGenerationWorker(_mockServiceProvider.Object, _mockLogger.Object);

		// Act
		await worker.ProcessJobAsync(jobId);

		// Assert
		delayCallCount.Should().Be(0, "First call should not trigger any delay");
		_mockAiService.Verify(a => a.GenerateJobScopeAsync(It.IsAny<JobPost>(), It.IsAny<string>(), It.IsAny<List<ServiceSku>>()), Times.Once);
	}

	[Fact]
	public async Task ProcessJobAsync_ShouldDelay_OnConsecutiveCallWithin30Seconds()
	{
		// Arrange
		var jobId = Guid.NewGuid();
		var jobPost = new JobPost { Id = jobId, ServiceCategoryId = Guid.NewGuid() };

		_mockUnitOfWork.Setup(u => u.JobPosts.GetByIdAsync(jobId))
			.ReturnsAsync(jobPost);
		_mockUnitOfWork.Setup(u => u.ServiceCategories.GetAllAsync())
			.ReturnsAsync(new List<ServiceCategory>());
		_mockUnitOfWork.Setup(u => u.ServiceSkus.GetByCategoryAsync(It.IsAny<Guid>()))
			.ReturnsAsync(new List<ServiceSku>());
		_mockUnitOfWork.Setup(u => u.JobTasks.GetTasksByJobPostAsync(jobId))
			.ReturnsAsync(new List<JobTask>());

		_mockAiService.Setup(a => a.GenerateJobScopeAsync(It.IsAny<JobPost>(), It.IsAny<string>(), It.IsAny<List<ServiceSku>>()))
			.ReturnsAsync(new AiScopeBreakdownResponse("Scope", new List<AiTaskBreakdownItem>()));

		var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

		// Simulate that the last call was just 10 seconds ago
		ScopeGenerationWorker._lastApiCallTime = baseTime;
		ScopeGenerationWorker.UtcNowProvider = () => baseTime.AddSeconds(10);

		int delayCallCount = 0;
		TimeSpan recordedDelay = TimeSpan.Zero;
		ScopeGenerationWorker.DelayTask = (ts) =>
		{
			delayCallCount++;
			recordedDelay = ts;
			return Task.CompletedTask;
		};

		var worker = new ScopeGenerationWorker(_mockServiceProvider.Object, _mockLogger.Object);

		// Act
		await worker.ProcessJobAsync(jobId);

		// Assert
		delayCallCount.Should().Be(1, "Consecutive call within 30s should trigger a delay");
		recordedDelay.Should().Be(TimeSpan.FromSeconds(20), "Delay should be exactly 20 seconds to enforce the 30s rate limit");

		// Cleanup statics so other tests aren't affected
		ScopeGenerationWorker.UtcNowProvider = () => DateTime.UtcNow;
		ScopeGenerationWorker.DelayTask = Task.Delay;
	}
}