using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using TaskStatus = BuildSmart.Core.Domain.Enums.TaskStatus;

namespace BuildSmart.Api.Tests.Services;

public class ProjectCategoryStatusManagementTests
{
    private AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task UpdateJobPostCategoryStatusAsync_ShouldAllowAdminToUpdateStatusAnytime()
    {
        // Arrange
        var dbName = $"CategoryStatusDb_{Guid.NewGuid()}";
        var projectId = Guid.NewGuid();
        var jobPostId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var homeownerUserId = Guid.NewGuid();

        using (var seedContext = CreateDbContext(dbName))
        {
            var profile = new HomeownerProfile { Id = Guid.NewGuid(), UserId = homeownerUserId };
            var project = new Project { Id = projectId, Title = "Test Project", Description = "Test Desc", HomeownerId = homeownerUserId, MasterOfferPdf = new byte[] { 1, 2, 3 } };
            var jobPost = new JobPost { Id = jobPostId, ProjectId = projectId, HomeownerProfileId = profile.Id, Title = "Drywall", Description = "Drywall desc", Location = "Sofia", CategoryStatus = ProjectCategoryStatus.Draft };

            await seedContext.HomeownerProfiles.AddAsync(profile);
            await seedContext.Projects.AddAsync(project);
            await seedContext.JobPosts.AddAsync(jobPost);
            await seedContext.SaveChangesAsync();
        }

        // Act
        using (var context = CreateDbContext(dbName))
        {
            var service = new ProjectManagementService(context);
            await service.UpdateJobPostCategoryStatusAsync(jobPostId, ProjectCategoryStatus.Active, adminUserId, UserRoleTypes.Admin);
        }

        // Assert
        using (var verifyContext = CreateDbContext(dbName))
        {
            var updatedJp = await verifyContext.JobPosts.FindAsync(jobPostId);
            var project = await verifyContext.Projects.FindAsync(projectId);

            updatedJp.Should().NotBeNull();
            updatedJp!.CategoryStatus.Should().Be(ProjectCategoryStatus.Active);
            project!.MasterOfferPdf.Should().BeNull(); // Master offer PDF cache invalidated
        }
    }

    [Fact]
    public async Task UpdateJobPostCategoryStatusAsync_ShouldAllowAssignedTradesmanToSubmitDraftForReview()
    {
        // Arrange
        var dbName = $"CategoryStatusDb_{Guid.NewGuid()}";
        var projectId = Guid.NewGuid();
        var jobPostId = Guid.NewGuid();
        var tradesmanUserId = Guid.NewGuid();
        var homeownerUserId = Guid.NewGuid();

        using (var seedContext = CreateDbContext(dbName))
        {
            var profile = new HomeownerProfile { Id = Guid.NewGuid(), UserId = homeownerUserId };
            var project = new Project { Id = projectId, Title = "Test Project", Description = "Test Desc", HomeownerId = homeownerUserId };
            var jobPost = new JobPost { Id = jobPostId, ProjectId = projectId, HomeownerProfileId = profile.Id, Title = "Plumbing", Description = "Plumbing desc", Location = "Sofia", CategoryStatus = ProjectCategoryStatus.Draft, AssignedTradesmanId = tradesmanUserId };

            await seedContext.HomeownerProfiles.AddAsync(profile);
            await seedContext.Projects.AddAsync(project);
            await seedContext.JobPosts.AddAsync(jobPost);
            await seedContext.SaveChangesAsync();
        }

        // Act
        using (var context = CreateDbContext(dbName))
        {
            var service = new ProjectManagementService(context);
            await service.UpdateJobPostCategoryStatusAsync(jobPostId, ProjectCategoryStatus.Pending, tradesmanUserId, UserRoleTypes.Tradesman);
        }

        // Assert
        using (var verifyContext = CreateDbContext(dbName))
        {
            var updatedJp = await verifyContext.JobPosts.FindAsync(jobPostId);
            updatedJp.Should().NotBeNull();
            updatedJp!.CategoryStatus.Should().Be(ProjectCategoryStatus.Pending);
        }
    }

    [Fact]
    public async Task GetProjectPaymentsBoardAsync_ShouldExcludeTasksFromDraftAndPendingCategories()
    {
        // Arrange
        var dbName = $"CategoryStatusDb_{Guid.NewGuid()}";
        var projectId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();
        var homeownerUserId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        using (var seedContext = CreateDbContext(dbName))
        {
            var profile = new HomeownerProfile { Id = Guid.NewGuid(), UserId = homeownerUserId };
            var serviceCategory = new ServiceCategory { Id = categoryId, Name = "General Work", TemplateStructure = "{}" };
            var project = new Project { Id = projectId, Title = "Payment Calculation Test Project", Description = "Test Desc", HomeownerId = homeownerUserId, AdminMarkupPercentage = 20m };

            var activeJp = new JobPost { Id = Guid.NewGuid(), ProjectId = projectId, HomeownerProfileId = profile.Id, ServiceCategoryId = categoryId, Title = "Active Category", Description = "Active desc", Location = "Sofia", CategoryStatus = ProjectCategoryStatus.Active };
            var activeTask = new JobTask { Id = Guid.NewGuid(), JobPostId = activeJp.Id, Title = "Active Task", Description = "Task desc", TradesmanPrice = 100m, EstimatedPrice = 120m, Status = TaskStatus.ToDo };
            activeJp.JobTasks.Add(activeTask);

            var pendingJp = new JobPost { Id = Guid.NewGuid(), ProjectId = projectId, HomeownerProfileId = profile.Id, ServiceCategoryId = categoryId, Title = "Pending Category", Description = "Pending desc", Location = "Sofia", CategoryStatus = ProjectCategoryStatus.Pending };
            var pendingTask = new JobTask { Id = Guid.NewGuid(), JobPostId = pendingJp.Id, Title = "Pending Task", Description = "Task desc", TradesmanPrice = 200m, EstimatedPrice = 240m, Status = TaskStatus.ToDo };
            pendingJp.JobTasks.Add(pendingTask);

            var draftJp = new JobPost { Id = Guid.NewGuid(), ProjectId = projectId, HomeownerProfileId = profile.Id, ServiceCategoryId = categoryId, Title = "Draft Category", Description = "Draft desc", Location = "Sofia", CategoryStatus = ProjectCategoryStatus.Draft };
            var draftTask = new JobTask { Id = Guid.NewGuid(), JobPostId = draftJp.Id, Title = "Draft Task", Description = "Task desc", TradesmanPrice = 300m, EstimatedPrice = 360m, Status = TaskStatus.ToDo };
            draftJp.JobTasks.Add(draftTask);

            await seedContext.HomeownerProfiles.AddAsync(profile);
            await seedContext.ServiceCategories.AddAsync(serviceCategory);
            await seedContext.Projects.AddAsync(project);
            await seedContext.JobPosts.AddRangeAsync(activeJp, pendingJp, draftJp);
            await seedContext.SaveChangesAsync();
        }

        // Act
        using (var context = CreateDbContext(dbName))
        {
            var service = new ProjectManagementService(context);
            var paymentsBoard = await service.GetProjectPaymentsBoardAsync(projectId, adminUserId, UserRoleTypes.Admin);

            // Assert: Only activeTask (€120 client total, €100 payout) should be included in payment calculations
            paymentsBoard.TotalProjectValue.Should().Be(120m);
            paymentsBoard.TotalUpcomingAmount.Should().Be(120m);
            paymentsBoard.TotalTradesmanPayoutValue.Should().Be(100m);
            paymentsBoard.UpcomingTasks.Should().HaveCount(1);
            paymentsBoard.UpcomingTasks.First().Title.Should().Be("Active Task");
        }
    }

    [Fact]
    public async Task GetProjectKanbanBoardAsync_ShouldFilterDraftCategoriesForHomeowner()
    {
        // Arrange
        var dbName = $"CategoryStatusDb_{Guid.NewGuid()}";
        var projectId = Guid.NewGuid();
        var homeownerUserId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        using (var seedContext = CreateDbContext(dbName))
        {
            var homeowner = new User { Id = homeownerUserId, Email = "ivan@test.com", FirstName = "Ivan", LastName = "Ivanov", Role = UserRoleTypes.Homeowner };
            var profile = new HomeownerProfile { Id = Guid.NewGuid(), UserId = homeownerUserId };
            homeowner.HomeownerProfile = profile;
            var serviceCategory = new ServiceCategory { Id = categoryId, Name = "General Work", TemplateStructure = "{}" };

            var project = new Project { Id = projectId, Title = "Kanban Filter Test Project", Description = "Test Desc", HomeownerId = homeownerUserId, Homeowner = homeowner };

            var activeJp = new JobPost { Id = Guid.NewGuid(), ProjectId = projectId, HomeownerProfileId = profile.Id, ServiceCategoryId = categoryId, Title = "Active Trade", Description = "Active desc", Location = "Sofia", CategoryStatus = ProjectCategoryStatus.Active };
            var pendingJp = new JobPost { Id = Guid.NewGuid(), ProjectId = projectId, HomeownerProfileId = profile.Id, ServiceCategoryId = categoryId, Title = "Pending Trade", Description = "Pending desc", Location = "Sofia", CategoryStatus = ProjectCategoryStatus.Pending };
            var draftJp = new JobPost { Id = Guid.NewGuid(), ProjectId = projectId, HomeownerProfileId = profile.Id, ServiceCategoryId = categoryId, Title = "Draft Trade", Description = "Draft desc", Location = "Sofia", CategoryStatus = ProjectCategoryStatus.Draft };

            await seedContext.Users.AddAsync(homeowner);
            await seedContext.HomeownerProfiles.AddAsync(profile);
            await seedContext.ServiceCategories.AddAsync(serviceCategory);
            await seedContext.Projects.AddAsync(project);
            await seedContext.JobPosts.AddRangeAsync(activeJp, pendingJp, draftJp);
            await seedContext.SaveChangesAsync();
        }

        // Act
        using (var context = CreateDbContext(dbName))
        {
            var service = new ProjectManagementService(context);
            var kanbanBoard = await service.GetProjectKanbanBoardAsync(projectId, homeownerUserId, UserRoleTypes.Homeowner);

            // Assert: Homeowner sees Active Trade and Pending Trade, but NOT Draft Trade
            kanbanBoard.CategorySections.Should().HaveCount(2);
            kanbanBoard.CategorySections.Select(s => s.CategoryStatus).Should().Contain(ProjectCategoryStatus.Active);
            kanbanBoard.CategorySections.Select(s => s.CategoryStatus).Should().Contain(ProjectCategoryStatus.Pending);
            kanbanBoard.CategorySections.Select(s => s.CategoryStatus).Should().NotContain(ProjectCategoryStatus.Draft);
        }
    }
}
