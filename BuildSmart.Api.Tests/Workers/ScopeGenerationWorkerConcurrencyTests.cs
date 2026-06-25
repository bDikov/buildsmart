using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BuildSmart.Api.Hubs;
using BuildSmart.Api.Workers;
using BuildSmart.Core.Application.DTOs;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Application.Resources;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
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
        var mockHubContext = new Mock<IHubContext<JobProcessingHub>>();
        var mockHubClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        var mockPricingEngine = new Mock<IPricingEngine>();

        mockHubContext.Setup(x => x.Clients).Returns(mockHubClients.Object);
        mockHubClients.Setup(x => x.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        
        var projectId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var homeownerId = Guid.NewGuid();
        
        var project = new Project { Id = projectId, LanguageCode = "en", HomeownerId = homeownerId };
        var jobPost1 = new JobPost { Id = Guid.NewGuid(), ProjectId = projectId, ServiceCategoryId = categoryId, Project = project };
        var jobPost2 = new JobPost { Id = Guid.NewGuid(), ProjectId = projectId, ServiceCategoryId = categoryId, Project = project };
        var jobPost3 = new JobPost { Id = Guid.NewGuid(), ProjectId = projectId, ServiceCategoryId = categoryId, Project = project };
        
        // Transition from Draft to GeneratingScope so they are realistically priced
        jobPost1.SubmitForScopeGeneration();
        jobPost2.SubmitForScopeGeneration();
        jobPost3.SubmitForScopeGeneration();

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
        var calcLock = new object();
        mockAiCalcRepo.Setup(r => r.AddAsync(It.IsAny<AiCalculation>()))
            .Callback<AiCalculation>(c =>
            {
                lock (calcLock)
                {
                    aiCalcs.Add(c);
                }
            })
            .Returns(Task.CompletedTask);

        mockAiCalcRepo.Setup(r => r.GetByProjectAsync(projectId)).ReturnsAsync(aiCalcs);
        mockAiCalcRepo.Setup(r => r.GetByProjectWithTasksAsync(projectId)).ReturnsAsync(aiCalcs);
        
        mockSkuRepo.Setup(r => r.GetByCategoryAsync(It.IsAny<Guid>())).ReturnsAsync(new List<ServiceSku>());

        mockAiService.Setup(a => a.CalculateTaskPricesAsync(It.IsAny<List<JobTask>>(), It.IsAny<List<ServiceSku>>(), It.IsAny<string>(), It.IsAny<string>(), System.Threading.CancellationToken.None))
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
        services.AddScoped(sp => mockHubContext.Object);
        services.AddScoped(sp => mockPricingEngine.Object);
        
        var serviceProvider = services.BuildServiceProvider();
        
        var worker = new ScopeGenerationWorker(serviceProvider, mockLogger.Object);
        
        // Act - Run 3 jobs concurrently to see if SemaphoreSlim prevents issues
        var task1 = worker.ProcessPricingAsync(jobPost1.Id, System.Threading.CancellationToken.None);
        var task2 = worker.ProcessPricingAsync(jobPost2.Id, System.Threading.CancellationToken.None);
        var task3 = worker.ProcessPricingAsync(jobPost3.Id, System.Threading.CancellationToken.None);
        
        var exception = await Record.ExceptionAsync(async () => await Task.WhenAll(task1, task2, task3));
        
        // Assert
        Assert.Null(exception); // Should not throw Concurrency Exceptions
        
        // Verify that GenerateOfferPdfAsync was called exactly once, confirming that the synchronization lock prevents duplicate PDF generation
        mockPdfService.Verify(p => p.GenerateOfferPdfAsync(It.IsAny<object>()), Times.Once());
    }

    [Fact]
    public async Task ProcessJobAsync_ShouldReturnEarly_WhenJobPostIsNotInGeneratingScopeOrRejected()
    {
        // Arrange
        var services = new ServiceCollection();
        
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockJobPostRepo = new Mock<IJobPostRepository>();
        var mockCategoryRepo = new Mock<IServiceCategoryRepository>();
        var mockSkuRepo = new Mock<IServiceSkuRepository>();
        var mockAiService = new Mock<IAiService>();
        var mockLogger = new Mock<ILogger<ScopeGenerationWorker>>();
        var mockHubContext = new Mock<IHubContext<JobProcessingHub>>();
        var mockHubClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        
        mockHubContext.Setup(x => x.Clients).Returns(mockHubClients.Object);
        mockHubClients.Setup(x => x.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

        var jobId = Guid.NewGuid();
        var jobPost = new JobPost { Id = jobId, Title = "Test Job" };
        
        // We set status to WaitingForUserReview, which is NOT GeneratingScope or Rejected
        jobPost.SubmitForScopeGeneration(); // transitions to GeneratingScope
        jobPost.CompletePricing();          // transitions to WaitingForUserReview
        
        mockUnitOfWork.Setup(u => u.JobPosts).Returns(mockJobPostRepo.Object);
        mockUnitOfWork.Setup(u => u.ServiceCategories).Returns(mockCategoryRepo.Object);
        mockUnitOfWork.Setup(u => u.ServiceSkus).Returns(mockSkuRepo.Object);
        
        mockJobPostRepo.Setup(r => r.GetByIdAsync(jobId)).ReturnsAsync(jobPost);
        mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ServiceCategory>());
        mockSkuRepo.Setup(r => r.GetByCategoryAsync(It.IsAny<Guid>())).ReturnsAsync(new List<ServiceSku>());

        services.AddScoped(sp => mockUnitOfWork.Object);
        services.AddScoped(sp => mockAiService.Object);
        services.AddScoped(sp => mockHubContext.Object);
        
        var serviceProvider = services.BuildServiceProvider();
        var worker = new ScopeGenerationWorker(serviceProvider, mockLogger.Object);

        // Act
        await worker.ProcessJobAsync(jobId, System.Threading.CancellationToken.None);

        // Assert
        // We expect that the AI service was NEVER called since status was not GeneratingScope/Rejected
        mockAiService.Verify(a => a.GenerateJobScopeAsync(
            It.IsAny<JobPost>(), 
            It.IsAny<string>(), 
            It.IsAny<List<ServiceSku>>(), 
            It.IsAny<string>(), 
            It.IsAny<System.Threading.CancellationToken>()), 
            Times.Never());
            
        // We expect the status to remain unchanged
        Assert.Equal(JobPostStatus.WaitingForUserReview, jobPost.Status);
    }

    [Fact]
    public async Task ProcessPricingAsync_ShouldReturnEarly_WhenJobPostIsNotInGeneratingScope()
    {
        // Arrange
        var services = new ServiceCollection();
        
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockJobPostRepo = new Mock<IJobPostRepository>();
        var mockCategoryRepo = new Mock<IServiceCategoryRepository>();
        var mockSkuRepo = new Mock<IServiceSkuRepository>();
        var mockAiService = new Mock<IAiService>();
        var mockPricingEngine = new Mock<IPricingEngine>();
        var mockNotificationService = new Mock<INotificationService>();
        var mockLogger = new Mock<ILogger<ScopeGenerationWorker>>();
        var mockHubContext = new Mock<IHubContext<JobProcessingHub>>();
        var mockHubClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        
        mockHubContext.Setup(x => x.Clients).Returns(mockHubClients.Object);
        mockHubClients.Setup(x => x.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

        var jobId = Guid.NewGuid();
        var jobPost = new JobPost { Id = jobId, Title = "Test Job" };
        
        // Put in WaitingForUserReview
        jobPost.SubmitForScopeGeneration();
        jobPost.CompletePricing();
        
        mockUnitOfWork.Setup(u => u.JobPosts).Returns(mockJobPostRepo.Object);
        mockUnitOfWork.Setup(u => u.ServiceCategories).Returns(mockCategoryRepo.Object);
        mockUnitOfWork.Setup(u => u.ServiceSkus).Returns(mockSkuRepo.Object);
        
        mockJobPostRepo.Setup(r => r.GetByIdWithTasksAsync(jobId)).ReturnsAsync(jobPost);
        mockCategoryRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ServiceCategory>());
        mockSkuRepo.Setup(r => r.GetByCategoryAsync(It.IsAny<Guid>())).ReturnsAsync(new List<ServiceSku>());

        services.AddScoped(sp => mockUnitOfWork.Object);
        services.AddScoped(sp => mockAiService.Object);
        services.AddScoped(sp => mockHubContext.Object);
        services.AddScoped(sp => mockPricingEngine.Object);
        services.AddScoped(sp => mockNotificationService.Object);
        
        var serviceProvider = services.BuildServiceProvider();
        var worker = new ScopeGenerationWorker(serviceProvider, mockLogger.Object);

        // Act
        await worker.ProcessPricingAsync(jobId, System.Threading.CancellationToken.None);

        // Assert
        // We expect that the pricing calculation was NEVER called since status was not GeneratingScope
        mockAiService.Verify(a => a.CalculateTaskPricesAsync(
            It.IsAny<List<JobTask>>(), 
            It.IsAny<List<ServiceSku>>(), 
            It.IsAny<string>(), 
            It.IsAny<string>(), 
            It.IsAny<System.Threading.CancellationToken>()), 
            Times.Never());
            
        // We expect the status to remain unchanged
        Assert.Equal(JobPostStatus.WaitingForUserReview, jobPost.Status);
    }

    [Fact]
    public async Task ProcessPricingAsync_ShouldSkipPdfGeneration_WhenNotAllCategoriesArePriced()
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
        var mockHubContext = new Mock<IHubContext<JobProcessingHub>>();
        var mockHubClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        var mockPricingEngine = new Mock<IPricingEngine>();

        mockHubContext.Setup(x => x.Clients).Returns(mockHubClients.Object);
        mockHubClients.Setup(x => x.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        
        var projectId = Guid.NewGuid();
        var categoryId1 = Guid.NewGuid();
        var categoryId2 = Guid.NewGuid();
        var homeownerId = Guid.NewGuid();
        
        var project = new Project { Id = projectId, LanguageCode = "en", HomeownerId = homeownerId };
        
        // 2 categories (job posts)
        var jobPost1 = new JobPost { Id = Guid.NewGuid(), ProjectId = projectId, ServiceCategoryId = categoryId1, Project = project };
        var jobPost2 = new JobPost { Id = Guid.NewGuid(), ProjectId = projectId, ServiceCategoryId = categoryId2, Project = project };
        
        jobPost1.SubmitForScopeGeneration();
        jobPost2.SubmitForScopeGeneration();

        var allJobs = new List<JobPost> { jobPost1, jobPost2 };
        
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
        
        mockJobPostRepo.Setup(r => r.GetByIdWithTasksAsync(jobPost1.Id)).ReturnsAsync(jobPost1);
        
        // Only 1 calculation in database (jobPost1 is being priced now, jobPost2 is NOT priced yet)
        var aiCalcs = new List<AiCalculation>();
        mockAiCalcRepo.Setup(r => r.AddAsync(It.IsAny<AiCalculation>()))
            .Callback<AiCalculation>(c => aiCalcs.Add(c))
            .Returns(Task.CompletedTask);
        mockAiCalcRepo.Setup(r => r.GetByProjectWithTasksAsync(projectId)).ReturnsAsync(aiCalcs);
        mockSkuRepo.Setup(r => r.GetByCategoryAsync(It.IsAny<Guid>())).ReturnsAsync(new List<ServiceSku>());

        mockAiService.Setup(a => a.CalculateTaskPricesAsync(It.IsAny<List<JobTask>>(), It.IsAny<List<ServiceSku>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiTaskPricingResponse(new List<AiTaskPricingItemDto>()));
            
        mockPdfService.Setup(p => p.GenerateOfferPdfAsync(It.IsAny<object>())).ReturnsAsync(new byte[] { 1, 2, 3 });

        var mockStringLocalizer = new Mock<IStringLocalizer<OfferResources>>();
        mockStringLocalizer.Setup(l => l[It.IsAny<string>()]).Returns(new LocalizedString("test", "test"));
        
        services.AddScoped(sp => mockUnitOfWork.Object);
        services.AddScoped(sp => mockAiService.Object);
        services.AddScoped(sp => mockPdfService.Object);
        services.AddScoped(sp => mockStringLocalizer.Object);
        services.AddScoped(sp => mockNotificationService.Object);
        services.AddScoped(sp => mockQueue.Object);
        services.AddScoped(sp => mockHubContext.Object);
        services.AddScoped(sp => mockPricingEngine.Object);
        
        var serviceProvider = services.BuildServiceProvider();
        var worker = new ScopeGenerationWorker(serviceProvider, mockLogger.Object);
        
        // Act
        await worker.ProcessPricingAsync(jobPost1.Id, CancellationToken.None);
        
        // Assert
        // Verify that PDF generation was NEVER called because jobPost2 is not yet priced
        mockPdfService.Verify(p => p.GenerateOfferPdfAsync(It.IsAny<object>()), Times.Never());
    }

    [Fact]
    public async Task ProcessPricingAsync_ShouldSkipPdfGeneration_WhenAnyCategoryIsRejected()
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
        var mockHubContext = new Mock<IHubContext<JobProcessingHub>>();
        var mockHubClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        var mockPricingEngine = new Mock<IPricingEngine>();

        mockHubContext.Setup(x => x.Clients).Returns(mockHubClients.Object);
        mockHubClients.Setup(x => x.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        
        var projectId = Guid.NewGuid();
        var categoryId1 = Guid.NewGuid();
        var categoryId2 = Guid.NewGuid();
        var homeownerId = Guid.NewGuid();
        
        var project = new Project { Id = projectId, LanguageCode = "en", HomeownerId = homeownerId };
        
        var jobPost1 = new JobPost { Id = Guid.NewGuid(), ProjectId = projectId, ServiceCategoryId = categoryId1, Project = project };
        var jobPost2 = new JobPost { Id = Guid.NewGuid(), ProjectId = projectId, ServiceCategoryId = categoryId2, Project = project };
        
        jobPost1.SubmitForScopeGeneration();
        
        // jobPost2 is Rejected (failed generation/pricing)
        jobPost2.SubmitForScopeGeneration();
        jobPost2.MarkGenerationFailed("AI Error");

        var allJobs = new List<JobPost> { jobPost1, jobPost2 };
        
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
        
        mockJobPostRepo.Setup(r => r.GetByIdWithTasksAsync(jobPost1.Id)).ReturnsAsync(jobPost1);
        
        // Calculations for BOTH categories exist, but jobPost2 is Rejected
        var calc1 = new AiCalculation { ProjectId = projectId, ServiceCategoryId = categoryId1 };
        var calc2 = new AiCalculation { ProjectId = projectId, ServiceCategoryId = categoryId2 };
        var aiCalcs = new List<AiCalculation> { calc1, calc2 };
        
        mockAiCalcRepo.Setup(r => r.GetByProjectWithTasksAsync(projectId)).ReturnsAsync(aiCalcs);
        mockSkuRepo.Setup(r => r.GetByCategoryAsync(It.IsAny<Guid>())).ReturnsAsync(new List<ServiceSku>());

        mockAiService.Setup(a => a.CalculateTaskPricesAsync(It.IsAny<List<JobTask>>(), It.IsAny<List<ServiceSku>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiTaskPricingResponse(new List<AiTaskPricingItemDto>()));
            
        mockPdfService.Setup(p => p.GenerateOfferPdfAsync(It.IsAny<object>())).ReturnsAsync(new byte[] { 1, 2, 3 });

        var mockStringLocalizer = new Mock<IStringLocalizer<OfferResources>>();
        mockStringLocalizer.Setup(l => l[It.IsAny<string>()]).Returns(new LocalizedString("test", "test"));
        
        services.AddScoped(sp => mockUnitOfWork.Object);
        services.AddScoped(sp => mockAiService.Object);
        services.AddScoped(sp => mockPdfService.Object);
        services.AddScoped(sp => mockStringLocalizer.Object);
        services.AddScoped(sp => mockNotificationService.Object);
        services.AddScoped(sp => mockQueue.Object);
        services.AddScoped(sp => mockHubContext.Object);
        services.AddScoped(sp => mockPricingEngine.Object);
        
        var serviceProvider = services.BuildServiceProvider();
        var worker = new ScopeGenerationWorker(serviceProvider, mockLogger.Object);
        
        // Act
        await worker.ProcessPricingAsync(jobPost1.Id, CancellationToken.None);
        
        // Assert
        // Verify that PDF generation was NEVER called because jobPost2 is Rejected
        mockPdfService.Verify(p => p.GenerateOfferPdfAsync(It.IsAny<object>()), Times.Never());
    }
}
