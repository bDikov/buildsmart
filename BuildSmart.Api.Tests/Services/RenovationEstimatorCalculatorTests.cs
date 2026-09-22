using System;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Application.Services;
using FluentAssertions;
using Xunit;

namespace BuildSmart.Api.Tests.Services;

public class RenovationEstimatorCalculatorTests
{
    private readonly RenovationEstimatorCalculator _calculator = new();

    [Fact]
    public void CalculateEstimate_StandardBathroom_OldBuilding_ShouldMatchLandingRates()
    {
        // Arrange
        var request = new RenovationEstimateRequest(
            Scope: "bathroom",
            SelectedArea: 4,
            BuildingStatus: "old",
            QualityTier: "standard",
            BathroomCount: 1,
            BathroomSizes: new[] { 4 },
            IncludeEquipment: true
        );

        // Act
        var result = _calculator.CalculateEstimate(request);

        // Assert
        // min = 4 * 420 + 1800 + 450 = 3930 EUR
        // max = 4 * 650 + 2600 + 650 = 5850 EUR
        result.MinPriceEur.Should().Be(3930m);
        result.MaxPriceEur.Should().Be(5850m);
        result.MinPriceBgn.Should().Be(Math.Round(3930m * 1.95583m, 0));
        result.MaxPriceBgn.Should().Be(Math.Round(5850m * 1.95583m, 0));
        result.EstimatedDays.Should().Be(22); // 12 + 5 + 5
        result.LaborMinEur.Should().Be(Math.Round(3930m * 0.48m, 0));
        result.RoughMaterialsMinEur.Should().Be(Math.Round(3930m * 0.27m, 0));
        result.FinishMaterialsMinEur.Should().Be(Math.Round(3930m * 0.25m, 0));
    }

    [Fact]
    public void CalculateEstimate_StandardBathroom_NewBuilding_ShouldExcludeDemolitionCost()
    {
        // Arrange
        var request = new RenovationEstimateRequest(
            Scope: "bathroom",
            SelectedArea: 4,
            BuildingStatus: "new",
            QualityTier: "standard",
            BathroomCount: 1,
            BathroomSizes: new[] { 4 },
            IncludeEquipment: true
        );

        // Act
        var result = _calculator.CalculateEstimate(request);

        // Assert: min = 4 * 420 + 1800 = 3480, max = 4 * 650 + 2600 = 5200
        result.MinPriceEur.Should().Be(3480m);
        result.MaxPriceEur.Should().Be(5200m);
        result.EstimatedDays.Should().Be(17); // 12 + 5 + 0
    }

    [Fact]
    public void CalculateEstimate_FullRenovation_70sqm_OldBuilding_ShouldMatchPerSqmRates()
    {
        // Arrange
        var request = new RenovationEstimateRequest(
            Scope: "full",
            SelectedArea: 70,
            BuildingStatus: "old",
            QualityTier: "standard",
            IncludeFurniture: false
        );

        // Act
        var result = _calculator.CalculateEstimate(request);

        // Assert: 70 * 450 = 31500, 70 * 540 = 37800
        result.MinPriceEur.Should().Be(31500m);
        result.MaxPriceEur.Should().Be(37800m);
        result.RatePerSqmMinEur.Should().Be(450m);
        result.RatePerSqmMaxEur.Should().Be(540m);
        result.EstimatedDays.Should().Be(88); // 40 + round(70 / 2.5) + 20 = 40 + 28 + 20 = 88
    }

    [Fact]
    public void CalculateEstimate_FullRenovation_WithFurniture_ShouldAddCustomCabinetry()
    {
        // Arrange
        var request = new RenovationEstimateRequest(
            Scope: "full",
            SelectedArea: 70,
            BuildingStatus: "old",
            QualityTier: "standard",
            IncludeFurniture: true
        );

        // Act
        var result = _calculator.CalculateEstimate(request);

        // Assert:
        // Base: 31500 - 37800 EUR
        // Furn: 70 * 230 = 16100, 70 * 350 = 24500 EUR
        // Total: 47600 - 62300 EUR
        result.MinPriceEur.Should().Be(47600m);
        result.MaxPriceEur.Should().Be(62300m);
    }

    [Fact]
    public void CalculateEstimate_FinishingScope_ShouldUseNewBuildingRates()
    {
        // Arrange
        var request = new RenovationEstimateRequest(
            Scope: "finishing",
            SelectedArea: 60,
            QualityTier: "standard"
        );

        // Act
        var result = _calculator.CalculateEstimate(request);

        // Assert: 60 * 370 = 22200, 60 * 440 = 26400
        result.MinPriceEur.Should().Be(22200m);
        result.MaxPriceEur.Should().Be(26400m);
    }

    [Theory]
    [InlineData("standard", 1.0)]
    [InlineData("premium", 1.25)]
    [InlineData("luxury", 1.85)]
    public void CalculateEstimate_QualityTiers_ShouldApplyCorrectMultipliers(string tier, decimal expectedMultiplier)
    {
        // Arrange
        var request = new RenovationEstimateRequest(
            Scope: "finishing",
            SelectedArea: 100,
            QualityTier: tier
        );

        // Act
        var result = _calculator.CalculateEstimate(request);

        // Assert: Base is 37000 min / 44000 max
        result.MinPriceEur.Should().Be(Math.Round(100 * 370m * expectedMultiplier, 0));
        result.MaxPriceEur.Should().Be(Math.Round(100 * 440m * expectedMultiplier, 0));
    }

    [Theory]
    [InlineData("Колко ще ми струва ремонт на баня 4 квадрата?", true)]
    [InlineData("Какви са цените за шпакловка и боядисване?", true)]
    [InlineData("Имате ли калкулатор за бюджет?", true)]
    [InlineData("Колко излиза на кв.м?", true)]
    [InlineData("What is the cost per sqm for renovation?", true)]
    [InlineData("Здравейте! Искам да попитам за оглед в четвъртък.", false)]
    [InlineData("Благодаря, ще очаквам обаждане.", false)]
    public void IsPricingOrDimensionQuery_ShouldDetectPricingIntentAccurately(string userMessage, bool expected)
    {
        var isQuery = _calculator.IsPricingOrDimensionQuery(userMessage);
        isQuery.Should().Be(expected);
    }

    [Fact]
    public void GetEstimatorKnowledgeSummary_ShouldContainKeyRatesAndPolicy()
    {
        var summary = _calculator.GetEstimatorKnowledgeSummary();

        summary.Should().Contain("https://buildsmart.bg/renovation-estimator");
        summary.Should().Contain("370");
        summary.Should().Contain("450");
        summary.Should().Contain("3,500");
        summary.Should().Contain("0% аванс");
    }
}
