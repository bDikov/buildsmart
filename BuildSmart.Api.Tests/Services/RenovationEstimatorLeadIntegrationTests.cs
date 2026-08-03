using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Infrastructure.Persistence.Repositories;
using BuildSmart.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildSmart.Api.Tests.Services;

public class RenovationEstimatorLeadIntegrationTests
{
    private DbContextOptions<AppDbContext> CreateNewContextOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"LeadIntegrationDb_{Guid.NewGuid()}")
            .Options;
    }

    [Fact]
    public async Task EndToEndLeadPipeline_ShouldVerifyEmail_PersistLead_AndDispatchOfferEmail()
    {
        // 1. Setup Services & InMemory Database
        var options = CreateNewContextOptions();
        var factoryMock = new TestDbContextFactory(options);
        
        var verificationService = new EmailVerificationService(NullLogger<EmailVerificationService>.Instance);
        var leadRepository = new CalculatorLeadRepository(factoryMock);

        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Smtp:Disabled", "true"},
            {"Smtp:Server", "localhost"},
            {"Smtp:Port", "25"}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var emailService = new EmailService(config, NullLogger<EmailService>.Instance, unitOfWork: null);

        // 2. Step 1: Verify Email Legitimacy
        string testEmail = "e2e_lead@example.com";
        var verificationResult = await verificationService.VerifyEmailAsync(testEmail);

        verificationResult.IsValid.Should().BeTrue();
        verificationResult.Status.Should().Be("Valid");

        // 3. Step 2: Create & Persist CalculatorLead with Marketing UTM Parameters
        var lead = new CalculatorLead
        {
            Id = Guid.NewGuid(),
            Email = testEmail,
            Name = "Димитър Димитров",
            Phone = "+359877112233",
            Scope = "full",
            SelectedArea = 95,
            BuildingStatus = "rough",
            QualityTier = "luxury",
            IncludeFurniture = true,
            IncludeEquipment = true,
            BathroomCount = 2,
            MinPriceEur = 57000m,
            MaxPriceEur = 85500m,
            MinPriceBgn = 111482m,
            MaxPriceBgn = 167223m,
            EstimatedDays = 125,
            IsEmailVerified = true,
            VerificationStatus = verificationResult.Status,
            UtmSource = "google",
            UtmMedium = "cpc",
            UtmCampaign = "sofia_renovation_2026_q3",
            UtmTerm = "remont_banya_sofia",
            UtmContent = "hero_lead_modal",
            CreatedAt = DateTime.UtcNow
        };

        await leadRepository.AddLeadAsync(lead);

        // 4. Step 3: Trigger Automated Email Dispatch
        Func<Task> emailAct = async () => await emailService.SendCalculatorLeadOfferEmailAsync(lead);
        await emailAct.Should().NotThrowAsync();

        // 5. Step 4: Verify Database State in an isolated DbContext (Rule 5 compliance)
        await using var verificationContext = new AppDbContext(options);
        var savedLead = await verificationContext.CalculatorLeads.FirstOrDefaultAsync(l => l.Id == lead.Id);

        savedLead.Should().NotBeNull();
        savedLead!.Email.Should().Be(testEmail);
        savedLead.Name.Should().Be("Димитър Димитров");
        savedLead.SelectedArea.Should().Be(95);
        savedLead.BuildingStatus.Should().Be("rough");
        savedLead.QualityTier.Should().Be("luxury");
        savedLead.MinPriceEur.Should().Be(57000m);
        savedLead.MaxPriceEur.Should().Be(85500m);
        savedLead.EstimatedDays.Should().Be(125);
        savedLead.UtmSource.Should().Be("google");
        savedLead.UtmMedium.Should().Be("cpc");
        savedLead.UtmCampaign.Should().Be("sofia_renovation_2026_q3");
        savedLead.UtmTerm.Should().Be("remont_banya_sofia");
        savedLead.UtmContent.Should().Be("hero_lead_modal");
    }

    [Fact]
    public async Task EndToEndLeadPipeline_ShouldReject_DisposableDomainLead()
    {
        var verificationService = new EmailVerificationService(NullLogger<EmailVerificationService>.Instance);
        var verificationResult = await verificationService.VerifyEmailAsync("disposable_user@mailinator.com");

        verificationResult.IsValid.Should().BeFalse();
        verificationResult.Status.Should().Be("DisposableDomain");
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
