using System;
using System.Linq;
using System.Threading.Tasks;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildSmart.Api.Tests.Services;

public class ProjectManagementServiceOfferTests
{
    private AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task AddCategoryToProjectAsync_ShouldAppendJobPostTasks_AndInvalidateMasterOfferPdf()
    {
        // Arrange
        var dbName = $"OfferTestDb_{Guid.NewGuid()}";
        var projectId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var homeownerUserId = Guid.NewGuid();

        using (var seedContext = CreateDbContext(dbName))
        {
            var homeowner = new User
            {
                Id = homeownerUserId,
                Email = "homeowner@test.com",
                FirstName = "Stefan",
                LastName = "Petrov",
                Role = UserRoleTypes.Homeowner
            };
            var profile = new HomeownerProfile { Id = Guid.NewGuid(), UserId = homeownerUserId };
            homeowner.HomeownerProfile = profile;
            await seedContext.Users.AddAsync(homeowner);
            await seedContext.HomeownerProfiles.AddAsync(profile);

            var category = new ServiceCategory
            {
                Id = categoryId,
                Name = "Flooring",
                TemplateStructure = "{}"
            };
            await seedContext.ServiceCategories.AddAsync(category);

            var sku1 = new ServiceSku
            {
                Id = Guid.NewGuid(),
                ServiceCategoryId = categoryId,
                SkuCode = "FLR-001",
                Name = "Parquet Installation",
                BasePrice = 39.1166m // ~20 EUR base price
            };
            await seedContext.ServiceSkus.AddAsync(sku1);

            var project = new Project
            {
                Id = projectId,
                Title = "Living Room Renovation",
                Description = "Sample project description",
                HomeownerId = homeownerUserId,
                AdminMarkupPercentage = 10.0m,
                MasterOfferPdf = new byte[] { 1, 2, 3 } // Cached PDF existing initially
            };
            await seedContext.Projects.AddAsync(project);
            await seedContext.SaveChangesAsync();
        }

        // Act
        using (var actContext = CreateDbContext(dbName))
        {
            var service = new ProjectManagementService(actContext);
            await service.AddCategoryToProjectAsync(projectId, categoryId);
        }

        // Assert
        using (var assertContext = CreateDbContext(dbName))
        {
            var project = await assertContext.Projects
                .Include(p => p.JobPosts)
                .ThenInclude(j => j.JobTasks)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            project.Should().NotBeNull();
            project!.MasterOfferPdf.Should().BeNull(); // MasterOfferPdf MUST be invalidated!
            project.JobPosts.Should().HaveCount(1);

            var jobPost = project.JobPosts.First();
            jobPost.ServiceCategoryId.Should().Be(categoryId);
            jobPost.JobTasks.Should().HaveCount(1);
            
            var task = jobPost.JobTasks.First();
            task.Title.Should().Be("Parquet Installation");
            task.EstimatedPrice.Should().BeGreaterThan(0m);
        }
    }

    [Fact]
    public async Task AddCategoryToProjectAsync_ShouldAutoCreateHomeownerProfile_IfMissing()
    {
        // Arrange
        var dbName = $"OfferTestDb_{Guid.NewGuid()}";
        var projectId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var homeownerUserId = Guid.NewGuid();

        using (var seedContext = CreateDbContext(dbName))
        {
            var homeowner = new User
            {
                Id = homeownerUserId,
                Email = "noprofile@test.com",
                FirstName = "No",
                LastName = "Profile",
                Role = UserRoleTypes.Homeowner
            };
            await seedContext.Users.AddAsync(homeowner); // No HomeownerProfile added

            var category = new ServiceCategory { Id = categoryId, Name = "Plumbing", TemplateStructure = "{}" };
            await seedContext.ServiceCategories.AddAsync(category);

            var project = new Project
            {
                Id = projectId,
                Title = "Plumbing Emergency",
                Description = "Plumbing Emergency Description",
                HomeownerId = homeownerUserId
            };
            await seedContext.Projects.AddAsync(project);
            await seedContext.SaveChangesAsync();
        }

        // Act
        using (var actContext = CreateDbContext(dbName))
        {
            var service = new ProjectManagementService(actContext);
            await service.AddCategoryToProjectAsync(projectId, categoryId);
        }

        // Assert
        using (var assertContext = CreateDbContext(dbName))
        {
            var profile = await assertContext.HomeownerProfiles.FirstOrDefaultAsync(h => h.UserId == homeownerUserId);
            profile.Should().NotBeNull(); // Profile should have been auto-created!
        }
    }
}
