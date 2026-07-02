using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using BuildSmart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Api.Tests
{
    public class LocalizationIntegrationTests : IClassFixture<TestApplicationFactory>
    {
        private readonly TestApplicationFactory _factory;

        public LocalizationIntegrationTests(TestApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Seeder_ShouldPopulateLocalizationResourcesFromAssembly()
        {
            // Arrange & Act
            // When TestApplicationFactory starts up, it runs Program.cs which triggers context.SeedLocalizationResourcesAsync()
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Assert
            var resources = await dbContext.LocalizationResources.ToListAsync();

            // We expect resources to be populated
            resources.Should().NotBeEmpty();

            // Verify both cultures "en" and "bg" are present
            var cultures = resources.Select(r => r.Culture).Distinct().ToList();
            cultures.Should().Contain(new[] { "en", "bg" });

            // Verify a specific key value for both languages
            var dashboardEn = resources.FirstOrDefault(r => r.Key == "Dashboard_Title" && r.Culture == "en");
            var dashboardBg = resources.FirstOrDefault(r => r.Key == "Dashboard_Title" && r.Culture == "bg");

            dashboardEn.Should().NotBeNull();
            dashboardEn!.Value.Should().Be("Dashboard");

            dashboardBg.Should().NotBeNull();
            dashboardBg!.Value.Should().Be("Табло");
        }
    }
}
