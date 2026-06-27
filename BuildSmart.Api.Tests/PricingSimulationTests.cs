using BuildSmart.Core.Application.DTOs;
using BuildSmart.Core.Application.Services;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BuildSmart.Api.Tests;

public class PricingSimulationTests
{
    private readonly ITestOutputHelper _output;

    public PricingSimulationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public class PricingTestCase
    {
        public string ScenarioName { get; set; } = string.Empty;
        public Dictionary<string, object> Answers { get; set; } = new();
        public List<string> QuestionCodes { get; set; } = new();
        public List<Guid> ActiveCategoryIds { get; set; } = new();
        public decimal ExpectedMinTotal { get; set; }
        public decimal ExpectedMaxTotal { get; set; }
        public List<string> ExpectedSkuCodes { get; set; } = new();
    }

    public static IEnumerable<object[]> GetTestScenarios()
    {
        yield return new object[]
        {
            new PricingTestCase
            {
                ScenarioName = "1. Painting Scenario - 100sqm Apartment with High Ceilings",
                QuestionCodes = new List<string>
                {
                    "global_property_type", "global_house_floors", "global_ceiling_height",
                    "global_total_sqm", "paint_tasks"
                },
                ActiveCategoryIds = new List<Guid>
                {
                    new Guid("ec7af4e8-16cb-4ec1-b618-35c739be9408"), // Global
                    new Guid("86ec6889-6adb-4800-81e9-e96d49fdd409")  // Paint
                },
                Answers = new Dictionary<string, object>
                {
                    { "global_property_type", "Апартамент" },
                    { "global_house_floors", "1" },
                    { "global_ceiling_height", "Висока (над 2.8м)" },
                    { "global_total_sqm", 100m },
                    { "paint_tasks", new[] { "Грундиране и боядисване" } }
                },
                ExpectedMinTotal = 2800m,
                ExpectedMaxTotal = 3100m,
                ExpectedSkuCodes = new List<string> { "GEN-002", "PANT-001", "PANT-003" }
            }
        };

        yield return new object[]
        {
            new PricingTestCase
            {
                ScenarioName = "2. Electrical Scenario - 3-Room Apartment with High-Power Appliances",
                QuestionCodes = new List<string>
                {
                    "global_property_type", "global_house_floors", "global_total_sqm", "global_room_count",
                    "elec_scope", "elec_heavy_appliances", "elec_ac_count", "elec_outlets_comfort"
                },
                ActiveCategoryIds = new List<Guid>
                {
                    new Guid("ec7af4e8-16cb-4ec1-b618-35c739be9408"), // Global
                    new Guid("38629ad2-757d-41ba-ac8f-18ddff053e89")  // Electrical
                },
                Answers = new Dictionary<string, object>
                {
                    { "global_property_type", "Апартамент" },
                    { "global_house_floors", "1" },
                    { "global_total_sqm", 80m },
                    { "global_room_count", 3m },
                    { "elec_scope", "Цялостна подмяна (всичко се изгражда наново)" },
                    { "elec_heavy_appliances", new[] { "Фурна", "Пералня", "Сушилня" } },
                    { "elec_ac_count", 2m },
                    { "elec_outlets_comfort", "Комфорт (по 5-6 на стая)" }
                },
                ExpectedMinTotal = 1500m,
                ExpectedMaxTotal = 1750m,
                ExpectedSkuCodes = new List<string> { "ELEC-CABLE-LAY", "ELEC-POINT-STD", "ELEC-PANEL-MOD" }
            }
        };

        yield return new object[]
        {
            new PricingTestCase
            {
                ScenarioName = "3. Drywall Scenario - 150sqm Suspended Ceiling & Insulation",
                QuestionCodes = new List<string>
                {
                    "global_property_type", "global_house_floors", "global_total_sqm",
                    "drywall_type", "drywall_insulation"
                },
                ActiveCategoryIds = new List<Guid>
                {
                    new Guid("ec7af4e8-16cb-4ec1-b618-35c739be9408"), // Global
                    new Guid("31cb956c-6362-4137-a58a-ab5b8efb7bee")  // Drywall
                },
                Answers = new Dictionary<string, object>
                {
                    { "global_property_type", "Апартамент" },
                    { "global_house_floors", "1" },
                    { "global_total_sqm", 150m },
                    { "drywall_type", "Окачен таван" },
                    { "drywall_insulation", "Да, минерална вата" }
                },
                ExpectedMinTotal = 180m,
                ExpectedMaxTotal = 220m,
                ExpectedSkuCodes = new List<string> { "GEN-002" }
            }
        };
    }

    [Theory]
    [MemberData(nameof(GetTestScenarios))]
    public async Task VerifyPricingAccuracy(PricingTestCase testCase)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Server=localhost;Port=5432;Database=buildsmart_db;Username=postgres;Password=postgres")
            .Options;

        using var context = new AppDbContext(options);
        var pricingEngine = new PricingEngine(new NullLogger<PricingEngine>());

        // 1. Fetch questions, formulas, and category templates
        var allQuestions = await context.Questions.ToListAsync();
        var allFormulas = await context.Formulas.ToListAsync();

        // 2. Setup answers dictionary
        var simulatedAnswers = new Dictionary<string, object>(testCase.Answers);

        // 3. Evaluate reusable formulas
        foreach (var formula in allFormulas)
        {
            try
            {
                var val = pricingEngine.CalculateQuantity(formula.Expression, JsonSerializer.Serialize(simulatedAnswers));
                simulatedAnswers[formula.Name] = val;
            }
            catch
            {
                // Reusable formulas might depend on variables not active in this specific category test case
            }
        }

        // 4. Fetch SKUs for active categories
        var skus = await context.ServiceSkus
            .Where(s => testCase.ActiveCategoryIds.Contains(s.ServiceCategoryId))
            .ToListAsync();

        var calculatedTasks = new List<CalculatedTaskDto>();
        decimal totalPriceSum = 0m;

        // 5. Evaluate SKU formulas
        var updatedJobDetails = JsonSerializer.Serialize(simulatedAnswers);
        foreach (var sku in skus)
        {
            if (string.IsNullOrWhiteSpace(sku.CalculationFormula)) continue;

            try
            {
                var qty = pricingEngine.CalculateQuantity(sku.CalculationFormula, updatedJobDetails);
                if (qty > 0)
                {
                    var basePrice = sku.BasePrice;
                    var totalPrice = basePrice * qty;

                    calculatedTasks.Add(new CalculatedTaskDto
                    {
                        SkuCode = sku.SkuCode,
                        Title = sku.Name,
                        Quantity = qty,
                        UnitType = sku.UnitType,
                        BasePrice = basePrice,
                        TotalPrice = totalPrice
                    });

                    totalPriceSum += totalPrice;
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"[WARNING] SKU '{sku.SkuCode}' calculation failed: {ex.Message}");
            }
        }

        // 6. Log output for diagnostics
        _output.WriteLine($"=================================================");
        _output.WriteLine($"SCENARIO: {testCase.ScenarioName}");
        _output.WriteLine($"=================================================");
        _output.WriteLine($"Grand Total: {totalPriceSum:C2}");
        _output.WriteLine($"");
        _output.WriteLine($"Tasks Calculated ({calculatedTasks.Count}):");
        foreach (var task in calculatedTasks.OrderBy(t => t.SkuCode))
        {
            _output.WriteLine($"  - {task.SkuCode}: {task.Title}");
            _output.WriteLine($"    Qty: {task.Quantity:F2} {task.UnitType} | Price: {task.BasePrice:C2} | Total: {task.TotalPrice:C2}");
        }

        // 7. Verify expected outputs
        Assert.True(totalPriceSum >= testCase.ExpectedMinTotal, 
            $"Grand total {totalPriceSum:C2} was below min threshold {testCase.ExpectedMinTotal:C2} for scenario {testCase.ScenarioName}");
        
        Assert.True(totalPriceSum <= testCase.ExpectedMaxTotal, 
            $"Grand total {totalPriceSum:C2} was above max threshold {testCase.ExpectedMaxTotal:C2} for scenario {testCase.ScenarioName}");

        foreach (var expectedSku in testCase.ExpectedSkuCodes)
        {
            Assert.Contains(calculatedTasks, t => t.SkuCode == expectedSku);
        }
    }
}
