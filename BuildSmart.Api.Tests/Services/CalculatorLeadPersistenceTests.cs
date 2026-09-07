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

    [Fact]
    public async Task UpdateLeadAsync_ShouldPersistAttributionAndFieldChanges()
    {
        // Arrange
        var options = CreateNewContextOptions();
        var factoryMock = new TestDbContextFactory(options);
        var repository = new CalculatorLeadRepository(factoryMock);

        var leadId = Guid.NewGuid();
        var lead = new CalculatorLead
        {
            Id = leadId,
            Email = "direct_lead@example.com",
            Phone = "+359888000111",
            Name = "Станко Станков",
            Scope = "partial",
            SelectedArea = 60,
            BuildingStatus = "old",
            QualityTier = "standard",
            IncludeFurniture = false,
            IncludeEquipment = false,
            BathroomCount = 1,
            MinPriceEur = 18000m,
            MaxPriceEur = 27000m,
            MinPriceBgn = 35205m,
            MaxPriceBgn = 52807m,
            EstimatedDays = 45,
            IsEmailVerified = true,
            VerificationStatus = "Valid",
            UtmSource = null, // Initially null (Direct / Organic)
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddLeadAsync(lead);

        // Act - Retroactively update attribution to Facebook Ads campaign
        lead.UtmSource = "facebook";
        lead.UtmMedium = "cpc";
        lead.UtmCampaign = "spring_promo_2026";
        lead.UtmContent = "carousel_ad_3";
        lead.Phone = "+359888999888";
        lead.IsContacted = true;
        lead.ContactedAt = DateTime.UtcNow;
        lead.AdminNotes = "Spoke to client, wants full kitchen + living room renovation in Sofia with premium materials.";

        await repository.UpdateLeadAsync(lead);

        // Assert - Rule 5: verify in separate scoped context
        await using var verificationContext = new AppDbContext(options);
        var updatedLead = await verificationContext.CalculatorLeads.FirstOrDefaultAsync(l => l.Id == leadId);

        updatedLead.Should().NotBeNull();
        updatedLead!.UtmSource.Should().Be("facebook");
        updatedLead.UtmMedium.Should().Be("cpc");
        updatedLead.UtmCampaign.Should().Be("spring_promo_2026");
        updatedLead.UtmContent.Should().Be("carousel_ad_3");
        updatedLead.Phone.Should().Be("+359888999888");
        updatedLead.IsContacted.Should().BeTrue();
        updatedLead.ContactedAt.Should().NotBeNull();
        updatedLead.AdminNotes.Should().Be("Spoke to client, wants full kitchen + living room renovation in Sofia with premium materials.");
        updatedLead.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteLeadAsync_ShouldRemoveLeadFromDatabase()
    {
        // Arrange
        var options = CreateNewContextOptions();
        var factoryMock = new TestDbContextFactory(options);
        var repository = new CalculatorLeadRepository(factoryMock);

        var leadId = Guid.NewGuid();
        var lead = new CalculatorLead
        {
            Id = leadId,
            Email = "delete_me@example.com",
            Scope = "full",
            SelectedArea = 70,
            BuildingStatus = "bds",
            QualityTier = "standard",
            IncludeFurniture = false,
            IncludeEquipment = false,
            BathroomCount = 1,
            MinPriceEur = 30000m,
            MaxPriceEur = 45000m,
            MinPriceBgn = 58675m,
            MaxPriceBgn = 88012m,
            EstimatedDays = 60,
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddLeadAsync(lead);

        // Verify it was added
        await using (var checkContext = new AppDbContext(options))
        {
            var exists = await checkContext.CalculatorLeads.AnyAsync(l => l.Id == leadId);
            exists.Should().BeTrue();
        }

        // Act
        await repository.DeleteLeadAsync(leadId);

        // Assert - Rule 5: verify in separate scoped context
        await using var verificationContext = new AppDbContext(options);
        var deletedLead = await verificationContext.CalculatorLeads.FirstOrDefaultAsync(l => l.Id == leadId);
        deletedLead.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnLead_WhenLeadExists()
    {
        // Arrange
        var options = CreateNewContextOptions();
        var factoryMock = new TestDbContextFactory(options);
        var repository = new CalculatorLeadRepository(factoryMock);

        var leadId = Guid.NewGuid();
        var lead = new CalculatorLead
        {
            Id = leadId,
            Email = "lookup@example.com",
            Name = "Ана Георгиева",
            Scope = "bathroom",
            SelectedArea = 8,
            BuildingStatus = "old",
            QualityTier = "luxury",
            IncludeFurniture = false,
            IncludeEquipment = false,
            BathroomCount = 1,
            MinPriceEur = 6000m,
            MaxPriceEur = 9000m,
            MinPriceBgn = 11735m,
            MaxPriceBgn = 17602m,
            EstimatedDays = 20,
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddLeadAsync(lead);

        // Act
        var result = await repository.GetByIdAsync(leadId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(leadId);
        result.Email.Should().Be("lookup@example.com");
        result.Name.Should().Be("Ана Георгиева");
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
