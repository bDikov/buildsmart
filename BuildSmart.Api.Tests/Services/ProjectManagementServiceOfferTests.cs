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

    [Fact]
    public async Task CreateProjectFromOfferTemplateAsync_ShouldApplyProjectAdminMarkupPercentage_ToCalculatedTaskPrices()
    {
        // Arrange
        var dbName = $"OfferTestDb_{Guid.NewGuid()}";
        var homeownerUserId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        using (var seedContext = CreateDbContext(dbName))
        {
            var homeowner = new User
            {
                Id = homeownerUserId,
                Email = "homeowner_markup@test.com",
                FirstName = "Hristo",
                LastName = "Hristov",
                Role = UserRoleTypes.Homeowner,
                HomeownerProfile = new HomeownerProfile { Id = Guid.NewGuid(), UserId = homeownerUserId }
            };
            await seedContext.Users.AddAsync(homeowner);

            var category = new ServiceCategory { Id = categoryId, Name = "Painting", TemplateStructure = "{}" };
            await seedContext.ServiceCategories.AddAsync(category);

            var sku = new ServiceSku
            {
                Id = Guid.NewGuid(),
                ServiceCategoryId = categoryId,
                SkuCode = "PAINT-01",
                Name = "Interior Painting",
                BasePrice = 12.50m
            };
            await seedContext.ServiceSkus.AddAsync(sku);
            await seedContext.SaveChangesAsync();
        }

        var phases = new List<BuildSmart.Core.Application.DTOs.CustomOfferPhaseDto>
        {
            new BuildSmart.Core.Application.DTOs.CustomOfferPhaseDto
            {
                PhaseTitle = "1. Interior Painting Phase",
                CategoryId = categoryId,
                CategoryName = "Painting",
                Items = new List<BuildSmart.Core.Application.DTOs.CustomOfferItemDto>
                {
                    new BuildSmart.Core.Application.DTOs.CustomOfferItemDto
                    {
                        SkuCode = "PAINT-01",
                        Title = "Interior Wall Painting",
                        Description = "Two coats of premium latex paint",
                        Unit = "м²",
                        Quantity = 100m,
                        UnitPriceEur = 10.00m // Base Total = €1,000.00
                    }
                }
            }
        };

        // Act
        Guid createdProjectId;
        using (var actContext = CreateDbContext(dbName))
        {
            var service = new ProjectManagementService(actContext);
            createdProjectId = await service.CreateProjectFromOfferTemplateAsync(
                homeownerUserId,
                "Lyulin Renovation Offer",
                "Full interior renovation",
                "Lyulin, Sofia",
                null,
                20.0m, // 20% Admin Markup
                phases);
        }

        // Assert
        using (var assertContext = CreateDbContext(dbName))
        {
            var project = await assertContext.Projects
                .Include(p => p.JobPosts)
                .ThenInclude(j => j.JobTasks)
                .FirstOrDefaultAsync(p => p.Id == createdProjectId);

            project.Should().NotBeNull();
            project!.AdminMarkupPercentage.Should().Be(20.0m);
            project.JobPosts.Should().HaveCount(1);

            var jobPost = project.JobPosts.First();
            jobPost.JobTasks.Should().HaveCount(1);

            var task = jobPost.JobTasks.First();
            task.TradesmanPrice.Should().Be(1000.00m); // Base Tradesman Total (100 * €10.00)
            task.EstimatedPrice.Should().Be(1200.00m); // Homeowner Client Price (€1,000.00 * 1.20)
        }
    }
}
