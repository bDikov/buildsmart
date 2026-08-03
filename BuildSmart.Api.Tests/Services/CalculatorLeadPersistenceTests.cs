using System;
using System.Threading.Tasks;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildSmart.Api.Tests.Services;

public class CalculatorLeadPersistenceTests
{
    private DbContextOptions<AppDbContext> CreateNewContextOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"CalculatorLeadDb_{Guid.NewGuid()}")
            .Options;
    }

    [Fact]
    public async Task AddLeadAsync_ShouldPersistCalculatorLead_WithUtmMetadata()
    {
        // Arrange
        var options = CreateNewContextOptions();
        var factoryMock = new TestDbContextFactory(options);
        var repository = new CalculatorLeadRepository(factoryMock);

        var lead = new CalculatorLead
        {
            Email = "client@example.com",
            Phone = "+359888123456",
            Name = "Георги Иванов",
            Scope = "full",
            SelectedArea = 85,
            BuildingStatus = "bds",
            QualityTier = "premium",
            IncludeFurniture = true,
            IncludeEquipment = true,
            BathroomCount = 1,
            MinPriceEur = 51000m,
            MaxPriceEur = 76500m,
            MinPriceBgn = 99747m,
            MaxPriceBgn = 149621m,
            EstimatedDays = 94,
            IsEmailVerified = true,
            VerificationStatus = "Valid",
            UtmSource = "google",
            UtmMedium = "cpc",
            UtmCampaign = "renovation_sofia_2026",
            UtmTerm = "remont_na_apartament_cena",
            UtmContent = "banner_lead_cta"
        };

        // Act
        await repository.AddLeadAsync(lead);

        // Assert - Verify using a fresh DbContext instance (Rule 5: Isolated context)
        await using var verificationContext = new AppDbContext(options);
        var savedLead = await verificationContext.CalculatorLeads.FirstOrDefaultAsync(l => l.Email == "client@example.com");

        savedLead.Should().NotBeNull();
        savedLead!.SelectedArea.Should().Be(85);
        savedLead.Scope.Should().Be("full");
        savedLead.QualityTier.Should().Be("premium");
        savedLead.MinPriceEur.Should().Be(51000m);
        savedLead.MaxPriceEur.Should().Be(76500m);
        savedLead.UtmSource.Should().Be("google");
        savedLead.UtmCampaign.Should().Be("renovation_sofia_2026");
        savedLead.UtmMedium.Should().Be("cpc");
        savedLead.IsEmailVerified.Should().BeTrue();
    }

    private class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        public AppDbContext CreateDbContext()
        {
            return new AppDbContext(_options);
        }
    }
}
