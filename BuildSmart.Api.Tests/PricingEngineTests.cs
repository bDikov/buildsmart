using System;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using BuildSmart.Infrastructure.Services;

namespace BuildSmart.Api.Tests;

public class PricingEngineTests
{
    private readonly PricingEngine _engine;

    public PricingEngineTests()
    {
        var logger = NullLogger<PricingEngine>.Instance;
        _engine = new PricingEngine(logger);
    }

    [Fact]
    public void CalculateQuantity_ShouldReturnZero_WhenFormulaIsEmpty()
    {
        var result = _engine.CalculateQuantity("", "{}");
        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateQuantity_ShouldReturnOne_WhenFormulaIsOne()
    {
        var result = _engine.CalculateQuantity("1", "{}");
        result.Should().Be(1m);
    }

    [Fact]
    public void CalculateQuantity_ShouldEvaluateBasicMath()
    {
        var json = @"{ ""global_total_sqm"": 100 }";
        var formula = "global_total_sqm * 3.5";
        
        var result = _engine.CalculateQuantity(formula, json);
        
        result.Should().Be(350m);
    }

    [Fact]
    public void CalculateQuantity_ShouldEvaluateContainsFunction_True()
    {
        var json = @"{ 
            ""global_total_sqm"": 50,
            ""elec_scope"": ""Цялостна подмяна""
        }";
        var formula = "if(Contains(elec_scope, 'Цялостна'), global_total_sqm * 2, 0)";
        
        var result = _engine.CalculateQuantity(formula, json);
        
        result.Should().Be(100m);
    }

    [Fact]
    public void CalculateQuantity_ShouldEvaluateContainsFunction_False()
    {
        var json = @"{ 
            ""global_total_sqm"": 50,
            ""elec_scope"": ""Частичен ремонт""
        }";
        var formula = "if(Contains(elec_scope, 'Цялостна'), global_total_sqm * 2, 15)";
        
        var result = _engine.CalculateQuantity(formula, json);
        
        result.Should().Be(15m);
    }

    [Fact]
    public void CalculateQuantity_ShouldEvaluateCountFunction_WithArray()
    {
        var json = @"{ 
            ""elec_heavy_appliances"": [""Фурна"", ""Пералня""]
        }";
        var formula = "Count(elec_heavy_appliances) * 10";
        
        var result = _engine.CalculateQuantity(formula, json);
        
        result.Should().Be(20m);
    }

    [Fact]
    public void CalculateQuantity_ShouldEvaluateCountFunction_WithCommaString()
    {
        var json = @"{ 
            ""elec_heavy_appliances"": ""Фурна, Пералня, Сушилня""
        }";
        var formula = "Count(elec_heavy_appliances) * 5";
        
        var result = _engine.CalculateQuantity(formula, json);
        
        result.Should().Be(15m);
    }

    [Fact]
    public void CalculateQuantity_ShouldDefaultMissingVariablesToZero()
    {
        var json = @"{ ""elec_scope"": ""Цялостна"" }";
        // global_total_sqm is missing from JSON
        var formula = "global_total_sqm * 3.5";
        
        var result = _engine.CalculateQuantity(formula, json);
        
        result.Should().Be(0m);
    }

    [Fact]
    public void CalculateQuantity_ShouldHandleComplexNestedIfs()
    {
        var json = @"{ 
            ""global_room_count"": 3,
            ""elec_outlets_comfort"": ""Комфорт""
        }";
        var formula = "if(Contains(elec_outlets_comfort, 'Базово'), (global_room_count * 3) + 4, if(Contains(elec_outlets_comfort, 'Комфорт'), (global_room_count * 5) + 6, 0))";
        
        var result = _engine.CalculateQuantity(formula, json);
        
        // (3 rooms * 5) + 6 = 21
        result.Should().Be(21m);
    }

    [Fact]
    public void CalculateQuantity_ShouldCalculateGerung_BasedOnBathroomCount()
    {
        var json = @"{ ""global_bathroom_count"": 2, ""tile_gerung"": ""Да, за всички външни ъгли"" }";
        var formula = "if(Contains(tile_gerung, 'Да'), global_bathroom_count * 6.0, 0)";
        
        var result = _engine.CalculateQuantity(formula, json);
        
        result.Should().Be(12.0m);
    }

    [Fact]
    public void CalculateQuantity_ShouldSumInsulation_Intelligently()
    {
        var json = @"{ 
            ""global_total_sqm"": 100, 
            ""drywall_type"": ""Окачен таван, Предстенна обшивка"",
            ""drywall_rooms"": ""В целия обект"",
            ""drywall_insulation"": ""Да, стандартна вата""
        }";
        
        // Formula sums ceiling (100) + lining (100 * 0.8) = 180
        var formula = "if(Contains(drywall_insulation, 'Не'), 0, (if(Contains(drywall_type, 'Окачен таван'), 100, 0) + if(Contains(drywall_type, 'Предстенна обшивка'), 100 * 0.8, 0)))";
        
        var result = _engine.CalculateQuantity(formula, json);
        
        result.Should().Be(180m);
    }

    [Fact]
    public void CalculateQuantity_ShouldApplyDemolitionFloorMultiplier_WhenNoElevator()
    {
        var json = @"{ 
            ""demo_disposal"": ""Да, искам контейнер и извозване"", 
            ""global_logistics"": ""Няма асансьор (качване по стълби)"",
            ""global_floor"": 5
        }";
        
        // 1 + (5 floors * 0.15) = 1.75 containers equivalent cost
        var formula = "if(Contains(demo_disposal, 'Да'), if(Contains(global_logistics, 'Няма асансьор'), 1 + (global_floor * 0.15), 1), 0)";
        
        var result = _engine.CalculateQuantity(formula, json);
        
        result.Should().Be(1.75m);
    }
}