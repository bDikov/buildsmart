using System;
using System.Collections.Generic;

namespace BuildSmart.Core.Application.DTOs;

public class GraphNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "question" | "sku" | "formula"
    public string Category { get; set; } = string.Empty;
}

public class GraphEdgeDto
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "question-to-sku" | "question-to-formula" | "question-to-question"
    public string? Label { get; set; }
}

public class OfferSimulationResultDto
{
    public List<CalculatedTaskDto> Tasks { get; set; } = new();
    public Dictionary<string, decimal> SkuQuantities { get; set; } = new();
    public Dictionary<string, decimal> PriceBreakdown { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public class CalculatedTaskDto
{
    public string SkuCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitType { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal TotalPrice { get; set; }
}
