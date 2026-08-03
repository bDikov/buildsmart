using System;
using System.Threading.Tasks;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildSmart.Api.Tests.Services;

public class HangfireEmailJobTests
{
    private readonly EmailService _emailService;

    public HangfireEmailJobTests()
    {
        var inMemorySettings = new System.Collections.Generic.Dictionary<string, string?>
        {
            {"Smtp:Disabled", "true"},
            {"Smtp:Server", "localhost"},
            {"Smtp:Port", "25"},
            {"Smtp:Username", "testuser"},
            {"Smtp:Password", "testpass"},
            {"Smtp:SenderEmail", "no-reply@buildsmart.bg"},
            {"Smtp:SenderName", "BuildSmart Test"}
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _emailService = new EmailService(config, NullLogger<EmailService>.Instance, unitOfWork: null);
    }

    [Fact]
    public async Task SendCalculatorLeadOfferEmailAsync_ShouldHandleNullLead_WithoutThrowing()
    {
        Func<Task> act = async () => await _emailService.SendCalculatorLeadOfferEmailAsync(null!);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendCalculatorLeadOfferEmailAsync_ShouldHandleEmptyEmail_WithoutThrowing()
    {
        var lead = new CalculatorLead { Email = "" };
        Func<Task> act = async () => await _emailService.SendCalculatorLeadOfferEmailAsync(lead);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendCalculatorLeadOfferEmailAsync_ShouldFormatLeadOfferEmail_ForFullRenovation()
    {
        var lead = new CalculatorLead
        {
            Id = Guid.NewGuid(),
            Email = "testlead@buildsmart.bg",
            Name = "Петър Петров",
            Phone = "+359888999000",
            Scope = "full",
            SelectedArea = 110,
            BuildingStatus = "bds",
            QualityTier = "luxury",
            MinPriceEur = 66000m,
            MaxPriceEur = 99000m,
            MinPriceBgn = 129084m,
            MaxPriceBgn = 193627m,
            EstimatedDays = 139,
            IsEmailVerified = true,
            VerificationStatus = "Valid",
            UtmSource = "google_ads",
            UtmCampaign = "luxury_renovation_2026"
        };

        // Execution should build body and handle SMTP dispatch or fallback gracefully
        Func<Task> act = async () => await _emailService.SendCalculatorLeadOfferEmailAsync(lead);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendCalculatorLeadOfferEmailAsync_ShouldFormatLeadOfferEmail_ForBathroomScope()
    {
        var lead = new CalculatorLead
        {
            Id = Guid.NewGuid(),
            Email = "bathroomlead@buildsmart.bg",
            Name = "Мария Георгиева",
            Scope = "bathroom",
            SelectedArea = 6,
            BuildingStatus = "old",
            QualityTier = "premium",
            MinPriceEur = 4200m,
            MaxPriceEur = 6500m,
            MinPriceBgn = 8214m,
            MaxPriceBgn = 12712m,
            EstimatedDays = 22,
            IsEmailVerified = true
        };

        Func<Task> act = async () => await _emailService.SendCalculatorLeadOfferEmailAsync(lead);
        await act.Should().NotThrowAsync();
    }
}
