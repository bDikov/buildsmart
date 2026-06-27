using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using BuildSmart.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

namespace BuildSmart.Api.Tests;

public class FormulaValidationTests
{
    private static readonly HashSet<string> ExpectedGlobalVariables = new(StringComparer.OrdinalIgnoreCase)
    {
        "global_property_type",
        "global_total_sqm",
        "global_ceiling_height",
        "global_room_count",
        "global_bathroom_count",
        "global_current_state",
        "global_logistics",
        "global_protection",
        "global_floor",
        "global_wall_material"
    };

    private string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "BuildSmart.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new Exception("Could not find solution root containing BuildSmart.sln");
    }

    [Fact]
    public void CategoriesSeedTemplates_ShouldBeSynchronizedAcrossDirectories()
    {
        // Arrange
        var rootDir = FindSolutionRoot();
        var rootTemplatesPath = Path.Combine(rootDir, "Categories_Seed_Templates.json");
        var infraTemplatesPath = Path.Combine(rootDir, "BuildSmart.Infrastructure", "Categories_Seed_Templates.json");

        // Assert files exist
        File.Exists(rootTemplatesPath).Should().BeTrue($"Root templates file must exist at: {rootTemplatesPath}");
        File.Exists(infraTemplatesPath).Should().BeTrue($"Infrastructure templates file must exist at: {infraTemplatesPath}");

        // Compare contents
        var rootContent = File.ReadAllText(rootTemplatesPath).Trim();
        var infraContent = File.ReadAllText(infraTemplatesPath).Trim();

        rootContent.Should().Be(infraContent, "because the root Categories_Seed_Templates.json and the one in BuildSmart.Infrastructure must be kept in sync.");
    }

    [Fact]
    public void SKUFormulas_ShouldOnlyReferenceValidQuestionIdsOrGlobalVariables()
    {
        // Arrange
        var rootDir = FindSolutionRoot();
        var templatesPath = Path.Combine(rootDir, "BuildSmart.Infrastructure", "Categories_Seed_Templates.json");
        File.Exists(templatesPath).Should().BeTrue();

        // 1. Load categories and their question IDs
        var categoryQuestionIds = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var globalQuestionIds = new HashSet<string>(ExpectedGlobalVariables, StringComparer.OrdinalIgnoreCase);

        var templatesJson = File.ReadAllText(templatesPath);
        using (var doc = JsonDocument.Parse(templatesJson))
        {
            foreach (var categoryProp in doc.RootElement.EnumerateObject())
            {
                var categoryName = categoryProp.Value.GetProperty("name").GetString();
                categoryName.Should().NotBeNull();

                var questionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var templateStructure = categoryProp.Value.GetProperty("templateStructure");
                if (templateStructure.TryGetProperty("questions", out var questionsArr))
                {
                    foreach (var question in questionsArr.EnumerateArray())
                    {
                        var qId = question.GetProperty("id").GetString();
                        if (!string.IsNullOrEmpty(qId))
                        {
                            questionIds.Add(qId);
                            if (categoryProp.Name == "global_category")
                            {
                                globalQuestionIds.Add(qId);
                            }
                        }
                    }
                }

                categoryQuestionIds[categoryName!] = questionIds;
            }
        }

        // 2. Load all SKU seed JSON files
        var infraDir = Path.Combine(rootDir, "BuildSmart.Infrastructure");
        var skuSeedFiles = Directory.GetFiles(infraDir, "*_SKUs_Seed.json");
        skuSeedFiles.Should().NotBeEmpty("SKU seed JSON files must exist in the infrastructure project");

        var errors = new List<string>();

        foreach (var file in skuSeedFiles)
        {
            var fileContent = File.ReadAllText(file);
            using var doc = JsonDocument.Parse(fileContent);
            var categoryName = doc.RootElement.GetProperty("categoryName").GetString();
            categoryName.Should().NotBeNull();

            // Find valid questions for this category (plus global questions)
            var categoryQuestions = categoryQuestionIds.TryGetValue(categoryName!, out var qSet) ? qSet : new HashSet<string>();

            var skusArr = doc.RootElement.GetProperty("skus");
            foreach (var sku in skusArr.EnumerateArray())
            {
                var skuCode = sku.GetProperty("skuCode").GetString();
                var formula = sku.GetProperty("calculationFormula").GetString();

                if (string.IsNullOrWhiteSpace(formula) || formula == "1" || formula == "0")
                {
                    continue;
                }

                var variables = ExtractVariables(formula);
                foreach (var variable in variables)
                {
                    bool isValid = globalQuestionIds.Contains(variable) || categoryQuestions.Contains(variable);
                    if (!isValid)
                    {
                        errors.Add($"SKU '{skuCode}' in category '{categoryName}' uses unknown variable '{variable}' in formula: '{formula}'");
                    }
                }
            }
        }

        // 3. Validate legacy formulas defined in AppDbContext.LegacySkuFormulas
        foreach (var legacyEntry in AppDbContext.LegacySkuFormulas)
        {
            var skuCode = legacyEntry.Key;
            var (formula, _, _) = legacyEntry.Value;

            if (string.IsNullOrWhiteSpace(formula) || formula == "1" || formula == "0")
            {
                continue;
            }

            // Map legacy prefix to category name
            string categoryName = skuCode switch
            {
                string s when s.StartsWith("PANT-") => "Бояджийски и шпакловъчни услуги",
                string s when s.StartsWith("TILE-") => "Подови и стенни настилки",
                string s when s.StartsWith("DEMO-") => "Къртене и извозване",
                _ => throw new Exception($"Unknown legacy SKU prefix: {skuCode}")
            };

            var categoryQuestions = categoryQuestionIds.TryGetValue(categoryName, out var qSet) ? qSet : new HashSet<string>();

            var variables = ExtractVariables(formula);
            foreach (var variable in variables)
            {
                bool isValid = globalQuestionIds.Contains(variable) || categoryQuestions.Contains(variable);
                if (!isValid)
                {
                    errors.Add($"Legacy SKU '{skuCode}' (Category: '{categoryName}') uses unknown variable '{variable}' in formula: '{formula}'");
                }
            }
        }

        // Assert all formulas are valid
        errors.Should().BeEmpty(string.Join(Environment.NewLine, errors));
    }

    private HashSet<string> ExtractVariables(string formula)
    {
        var variables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // Remove string literals to prevent matching text inside quotes
        var cleanedFormula = Regex.Replace(formula, @"'[^']*'|""[^""]*""", "");
        
        // Find all word tokens
        var matches = Regex.Matches(cleanedFormula, @"\b[a-zA-Z_][a-zA-Z0-9_]*\b");
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "if", "contains", "count", "ceiling", "floor", "round", "and", "or", "not", "true", "false", "in"
        };

        foreach (Match match in matches)
        {
            var word = match.Value;
            if (!keywords.Contains(word))
            {
                variables.Add(word);
            }
        }
        return variables;
    }
}
