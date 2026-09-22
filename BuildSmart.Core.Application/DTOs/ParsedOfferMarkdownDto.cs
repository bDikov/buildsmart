using System;
using System.Collections.Generic;

namespace BuildSmart.Core.Application.DTOs;

public class OfferPreviewDto
{
    public string ProjectTitle { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string? ClientEmail { get; set; }
    public string? ClientPhone { get; set; }
    public string? SiteAddress { get; set; }
    public string AssignTo { get; set; } = "admin";
    public string Currency { get; set; } = "EUR";
    public decimal AdminMarkupPercentage { get; set; } = 0.0m;
    public string ScopeDescription { get; set; } = string.Empty;

    public List<OfferPreviewSectionDto> Sections { get; set; } = new();

    public decimal GrandTotalEur { get; set; }
    public decimal GrandTotalBgn => Math.Round(GrandTotalEur * 1.95583m, 2);

    public string? GrandTotalRangeText { get; set; }
    public string? GrandTotalBgnRangeText { get; set; }
    public decimal GrandTotalMinEur { get; set; }
    public decimal GrandTotalMaxEur { get; set; }
    public string? AveragePricePerSqm { get; set; }

    public ProjectParametersDto Parameters { get; set; } = new();
    public List<InvestmentComponentDto> InvestmentBreakdown { get; set; } = new();
    public List<EngineeringNoteDto> EngineeringNotes { get; set; } = new();

    public bool IsValid { get; set; } = true;
    public int TotalItemCount { get; set; }
    public int MatchedSkuCount { get; set; }
    public int CustomItemCount { get; set; }
    public List<string> ValidationMessages { get; set; } = new();
}

public class ProjectParametersDto
{
    public string? Location { get; set; }
    public string? PropertyCondition { get; set; }
    public string? TotalArea { get; set; }
    public string? ExecutionTimeline { get; set; }
    public string? QualityLevel { get; set; }
    public string? Warranty { get; set; }
    public string? PaymentTerms { get; set; }
    public Dictionary<string, string> AdditionalDetails { get; set; } = new();
    public bool HasParameters => !string.IsNullOrEmpty(Location) || !string.IsNullOrEmpty(TotalArea) || !string.IsNullOrEmpty(ExecutionTimeline) || !string.IsNullOrEmpty(Warranty);
}

public class InvestmentComponentDto
{
    public string Name { get; set; } = string.Empty;
    public string SharePercentage { get; set; } = string.Empty;
    public string AmountEurRange { get; set; } = string.Empty;
    public string AmountBgnRange { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class EngineeringNoteDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? CostEstimate { get; set; }
    public string Type { get; set; } = "note"; // "note", "tip", "recommendation"
}

public class OfferPreviewSectionDto
{
    public string CategoryName { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public List<OfferPreviewItemDto> Items { get; set; } = new();
    public decimal SubtotalEur { get; set; }
    public string? SubtotalRangeText { get; set; }
}

public class OfferPreviewItemDto
{
    public int Index { get; set; }
    public string SkuCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Unit { get; set; } = "бр.";
    public decimal Quantity { get; set; }
    public decimal BaseUnitPriceEur { get; set; }
    public decimal EffectiveUnitPriceEur { get; set; }
    public decimal TotalEur { get; set; }
    public string? PriceRangeText { get; set; }
    public decimal? MinUnitPriceEur { get; set; }
    public decimal? MaxUnitPriceEur { get; set; }
    public List<string> SubItems { get; set; } = new();
    public bool IsSkuRecognized { get; set; }
    public bool IsCustomPrice { get; set; }
    public string? ValidationMessage { get; set; }
}

public class ParsedOfferMarkdownDto
{
    public string ProjectTitle { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string? ClientEmail { get; set; }
    public string? ClientPhone { get; set; }
    public string? SiteAddress { get; set; }
    public string AssignTo { get; set; } = "admin";
    public decimal AdminMarkupPercentage { get; set; } = 0.0m;
    public string Currency { get; set; } = "EUR";
    public string Language { get; set; } = "bg";
    public string ScopeDescription { get; set; } = string.Empty;

    public string? GrandTotalRangeText { get; set; }
    public string? GrandTotalBgnRangeText { get; set; }
    public decimal GrandTotalMinEur { get; set; }
    public decimal GrandTotalMaxEur { get; set; }
    public string? AveragePricePerSqm { get; set; }

    public ProjectParametersDto Parameters { get; set; } = new();
    public List<InvestmentComponentDto> InvestmentBreakdown { get; set; } = new();
    public List<EngineeringNoteDto> EngineeringNotes { get; set; } = new();

    public List<CustomOfferPhaseDto> Phases { get; set; } = new();
    public List<string> ValidationMessages { get; set; } = new();
    public bool IsValid => ValidationMessages.Count == 0;
}

public class MarkdownOfferItemOverride
{
    public int Index { get; set; }
    public string? SkuCode { get; set; }
    public string? Title { get; set; }
    public decimal? UnitPriceEur { get; set; }
    public string? PriceRangeText { get; set; }
    public decimal? MinUnitPriceEur { get; set; }
    public decimal? MaxUnitPriceEur { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? TotalEur { get; set; }
}
