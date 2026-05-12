using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BuildSmart.Api.Workers;
using BuildSmart.Core.Application.DTOs;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Application.Resources;
using BuildSmart.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BuildSmart.Api.Tests.Workers;

public class ScopeGenerationWorkerConcurrencyTests
{
    [Fact]
    public async Task ProcessPricingAsync_ShouldHandleConcurrentExecutionsSafely()
    {
        // Arrange
        var services = new ServiceCollection();
        
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockJobPostRepo = new Mock<IJobPostRepository>();
        var mockProjectRepo = new Mock<IProjectRepository>();
        var mockAiCalcRepo = new Mock<IAiCalculationRepository>();
        var mockUserRepo = new Mock<IUserRepository>();
        var mockSkuRepo = new Mock<IServiceSkuRepository>();
        var mockCategoryRepo = new Mock<IServiceCategoryRepository>();
        
        var mockAiService = new Mock<IAiService>();
        var mockPdfService = new Mock<IPdfGeneratorService>();
        var mockNotificationService = new Mock<INotificationService>();
        var mockQueue = new Mock<IScopeGenerationQueue>();
        var mockLogger = new Mock<ILogger<ScopeGenerationWorker>>();
        
        var projectId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var homeownerId = Guid.NewGuid();
        
        var project = new Project { Id = projectId, LanguageCode = "en", HomeownerId = homeownerId };
        var jobPost1 = new JobPost { Id = Guid.NewGuid(), ProjectId = projectId, ServiceCategoryId = categoryId, Project = project };
        var jobPost2 = new JobPost { Id = Guid.NewGuid(), ProjectId = projectId, ServiceCategoryId = categoryId, Project = project };
        var jobPost3 = new JobPost { Id = Guid.NewGuid(), ProjectId = projectId, ServiceCategoryId = categoryId, Project = project };
        
        var allJobs = new List<JobPost> { jobPost1, jobPost2, jobPost3 };
        
        // Mocks for UnitOfWork
        mockUnitOfWork.Setup(u => u.Projects).Returns(mockProjectRepo.Object);
        mockUnitOfWork.Setup(u => u.JobPosts).Returns(mockJobPostRepo.Object);
        mockUnitOfWork.Setup(u => u.AiCalculations).Returns(mockAiCalcRepo.Object);
        mockUnitOfWork.Setup(u => u.ServiceSkus).Returns(mockSkuRepo.Object);
        mockUnitOfWork.Setup(u => u.Users).Returns(mockUserRepo.Object);
        mockUnitOfWork.Setup(u => u.ServiceCategories).Returns(mockCategoryRepo.Object);
        
        mockProjectRepo.Setup(r => r.GetByIdAsync(projectId)).ReturnsAsync(project);
        mockJobPostRepo.Setup(r => r.GetJobsByProjectIdAsync(projectId)).ReturnsAsync(allJobs);
        mockUserRepo.Setup(r => r.GetByIdAsync(homeownerId)).ReturnsAsync(new User { Id = homeownerId, PreferredLanguage = "en" });
        mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ServiceCategory>());
        
        // Setup getting jobs
        mockJobPostRepo.Setup(r => r.GetByIdWithTasksAsync(jobPost1.Id)).ReturnsAsync(jobPost1);
        mockJobPostRepo.Setup(r => r.GetByIdWithTasksAsync(jobPost2.Id)).ReturnsAsync(jobPost2);
        mockJobPostRepo.Setup(r => r.GetByIdWithTasksAsync(jobPost3.Id)).ReturnsAsync(jobPost3);
        
        var aiCalcs = new List<AiCalculation>();
        mockAiCalcRepo.Setup(r => r.GetByProjectAsync(projectId)).ReturnsAsync(aiCalcs);
        mockAiCalcRepo.Setup(r => r.GetByProjectWithTasksAsync(projectId)).ReturnsAsync(aiCalcs);
        
        mockSkuRepo.Setup(r => r.GetByCategoryAsync(It.IsAny<Guid>())).ReturnsAsync(new List<ServiceSku>());

        mockAiService.Setup(a => a.CalculateTaskPricesAsync(It.IsAny<List<JobTask>>(), It.IsAny<List<ServiceSku>>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new AiTaskPricingResponse(new List<AiTaskPricingItemDto>())); // Emulate successful empty tasks
            
        mockPdfService.Setup(p => p.GenerateOfferPdfAsync(It.IsAny<object>())).ReturnsAsync(new byte[] { 1, 2, 3 });

        var mockStringLocalizer = new Mock<IStringLocalizer<OfferResources>>();
        mockStringLocalizer.Setup(l => l[It.IsAny<string>()]).Returns(new LocalizedString("test", "test"));
        
        services.AddScoped(sp => mockUnitOfWork.Object);
        services.AddScoped(sp => mockAiService.Object);
        services.AddScoped(sp => mockPdfService.Object);
        services.AddScoped(sp => mockStringLocalizer.Object);
        services.AddScoped(sp => mockNotificationService.Object);
        services.AddScoped(sp => mockQueue.Object);
        
        var serviceProvider = services.BuildServiceProvider();
        
        var worker = new ScopeGenerationWorker(serviceProvider, mockLogger.Object);
        
        // Act - Run 3 jobs concurrently to see if SemaphoreSlim prevents issues
        var task1 = worker.ProcessPricingAsync(jobPost1.Id);
        var task2 = worker.ProcessPricingAsync(jobPost2.Id);
        var task3 = worker.ProcessPricingAsync(jobPost3.Id);
        
        var exception = await Record.ExceptionAsync(async () => await Task.WhenAll(task1, task2, task3));
        
        // Assert
        Assert.Null(exception); // Should not throw Concurrency Exceptions
        
        // Verify that GenerateOfferPdfAsync was called exactly 3 times, meaning all 3 workers got their turn in the lock
        mockPdfService.Verify(p => p.GenerateOfferPdfAsync(It.IsAny<object>()), Times.Exactly(3));
    }
}
