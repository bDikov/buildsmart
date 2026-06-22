using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;

namespace BuildSmart.Api.Tests
{
    public class OffersIntegrationTests : IClassFixture<TestApplicationFactory>
    {
        private readonly TestApplicationFactory _factory;

        public OffersIntegrationTests(TestApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task DownloadOfferPdf_ShouldReturnBadRequest_WhenAnyCategoryIsDraft_Integration()
        {
            // Arrange
            var client = _factory.CreateClient();
            var projectId = Guid.NewGuid();
            var homeownerId = Guid.NewGuid();
            var profileId = Guid.NewGuid();

            // Seed database using a fresh scope
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var homeowner = new User
                {
                    Id = homeownerId,
                    Email = $"homeowner-{Guid.NewGuid()}@example.com",
                    FirstName = "John",
                    LastName = "Doe",
                    HashedPassword = "password",
                    Role = UserRoleTypes.Homeowner
                };
                var homeownerProfile = new HomeownerProfile
                {
                    Id = profileId,
                    UserId = homeownerId
                };
                homeowner.HomeownerProfile = homeownerProfile;

                await dbContext.Users.AddAsync(homeowner);
                await dbContext.HomeownerProfiles.AddAsync(homeownerProfile);

                var categoryId = Guid.NewGuid();
                var category = new ServiceCategory
                {
                    Id = categoryId,
                    Name = "Kitchen Cabinets",
                    TemplateStructure = "{}"
                };
                await dbContext.ServiceCategories.AddAsync(category);

                var project = new Project
                {
                    Id = projectId,
                    Title = "Kitchen Remodel",
                    Description = "Demo project",
                    HomeownerId = homeownerId
                };

                var jobPost = new JobPost
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    ServiceCategoryId = categoryId,
                    Title = "Kitchen Cabinets",
                    Location = "Sofia",
                    Description = "Installing new cabinets",
                    HomeownerProfileId = profileId
                };
                // Job post remains in Draft status by default
                project.JobPosts.Add(jobPost);

                await dbContext.Projects.AddAsync(project);
                await dbContext.SaveChangesAsync();
            }

            // Act
            var response = await client.GetAsync($"/api/Offers/{projectId}/download");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Cannot download offer until all categories are filled out.");
        }
    }
}
