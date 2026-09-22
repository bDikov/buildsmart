using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BuildSmart.Core.Application.DTOs;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildSmart.Infrastructure.Services;

public class MarkdownOfferParserService : IMarkdownOfferParserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<MarkdownOfferParserService> _logger;

    public MarkdownOfferParserService(AppDbContext context, ILogger<MarkdownOfferParserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<OfferPreviewDto> PreviewMarkdownOfferAsync(string markdownContent)
    {
        var rawData = ParseMarkdownRaw(markdownContent);
        var preview = new OfferPreviewDto
        {
            ProjectTitle = rawData.ProjectTitle,
            ClientName = rawData.ClientName,
            ClientEmail = rawData.ClientEmail,
            ClientPhone = rawData.ClientPhone,
            SiteAddress = rawData.SiteAddress,
            AssignTo = rawData.AssignTo,
            Currency = rawData.Currency,
            AdminMarkupPercentage = rawData.AdminMarkupPercentage,
            ScopeDescription = rawData.ScopeDescription,
            GrandTotalRangeText = rawData.GrandTotalRangeText,
            GrandTotalBgnRangeText = rawData.GrandTotalBgnRangeText,
            GrandTotalMinEur = rawData.GrandTotalMinEur,
            GrandTotalMaxEur = rawData.GrandTotalMaxEur,
            AveragePricePerSqm = rawData.AveragePricePerSqm,
            Parameters = rawData.Parameters,
            InvestmentBreakdown = rawData.InvestmentBreakdown,
            EngineeringNotes = rawData.EngineeringNotes
        };

        var allSkus = await _context.ServiceSkus
            .Include(s => s.ServiceCategory)
            .AsNoTracking()
            .ToListAsync();

        var allCategories = await _context.ServiceCategories
            .AsNoTracking()
            .ToListAsync();

        decimal markupFactor = 1.0m + (rawData.AdminMarkupPercentage / 100.0m);
        decimal grandTotalEur = 0m;
        int totalItems = 0;
        int matchedItems = 0;
        int customItems = 0;
        int itemIndex = 1;

        foreach (var rawSection in rawData.RawSections)
        {
            var matchedCategory = MatchCategory(allCategories, rawSection.CategoryName);

            var sectionDto = new OfferPreviewSectionDto
            {
                CategoryName = matchedCategory?.Name ?? rawSection.CategoryName,
                CategoryId = matchedCategory?.Id
            };

            decimal sectionSubtotalEur = 0m;

            foreach (var rawItem in rawSection.RawItems)
            {
                totalItems++;
                var matchedSku = MatchSku(allSkus, rawItem.SkuCode, rawItem.Title);

                bool isRecognized = matchedSku != null;
                bool isCustomPrice = false;
                string? validationMsg = null;
                decimal baseUnitPriceEur;

                if (rawItem.CustomUnitPriceEur.HasValue && rawItem.CustomUnitPriceEur.Value > 0)
                {
                    baseUnitPriceEur = rawItem.CustomUnitPriceEur.Value;
                    if (isRecognized)
                    {
                        matchedItems++;
                    }
                    else
                    {
                        isCustomPrice = true;
                        customItems++;
                    }
                }
                else if (matchedSku != null)
                {
                    baseUnitPriceEur = Math.Round(matchedSku.BasePrice / 1.95583m, 2);
                    matchedItems++;
                }
                else
                {
                    baseUnitPriceEur = 0m;
                    validationMsg = !string.IsNullOrWhiteSpace(rawItem.SkuCode)
                        ? $"Перо с код '{rawItem.SkuCode}' не е намерено в номенклатурата на BuildSmart."
                        : $"Перо '{rawItem.Title}' няма съответстващ SKU код и липсва единична цена.";
                    preview.ValidationMessages.Add(validationMsg);
                }

                decimal tradesmanTotalEur = Math.Round(rawItem.Quantity * baseUnitPriceEur, 2);
                decimal clientTotalEur = Math.Round(tradesmanTotalEur * markupFactor, 2);
                decimal clientUnitPriceEur = rawItem.Quantity > 0 ? Math.Round(clientTotalEur / rawItem.Quantity, 2) : 0m;

                string normalizedUnit = NormalizeUnit(rawItem.Unit, matchedSku?.UnitType);
                string resolvedSkuCode = matchedSku?.SkuCode 
                    ?? (!string.IsNullOrWhiteSpace(rawItem.SkuCode) ? rawItem.SkuCode : (isCustomPrice ? $"CUSTOM-{itemIndex:D2}" : string.Empty));

                var itemDto = new OfferPreviewItemDto
                {
                    Index = itemIndex++,
                    SkuCode = resolvedSkuCode,
                    Title = !string.IsNullOrWhiteSpace(rawItem.Title) ? rawItem.Title : (matchedSku?.Name ?? rawItem.SkuCode),
                    Unit = normalizedUnit,
                    Quantity = rawItem.Quantity,
                    BaseUnitPriceEur = baseUnitPriceEur,
                    EffectiveUnitPriceEur = clientUnitPriceEur,
                    TotalEur = clientTotalEur,
                    PriceRangeText = rawItem.PriceRangeText,
                    MinUnitPriceEur = rawItem.MinPriceEur,
                    MaxUnitPriceEur = rawItem.MaxPriceEur,
                    SubItems = rawItem.SubItems != null ? new List<string>(rawItem.SubItems) : new List<string>(),
                    IsSkuRecognized = isRecognized,
                    IsCustomPrice = isCustomPrice,
                    ValidationMessage = validationMsg
                };

                sectionSubtotalEur += clientTotalEur;
                sectionDto.Items.Add(itemDto);
            }

            sectionDto.SubtotalEur = sectionSubtotalEur;
            grandTotalEur += sectionSubtotalEur;
            preview.Sections.Add(sectionDto);
        }

        preview.GrandTotalEur = grandTotalEur;
        preview.TotalItemCount = totalItems;
        preview.MatchedSkuCount = matchedItems;
        preview.CustomItemCount = customItems;
        preview.IsValid = preview.ValidationMessages.Count == 0 && (matchedItems + customItems) > 0;

        // If no explicit grand total range was set, compute it from items
        if (string.IsNullOrEmpty(preview.GrandTotalRangeText))
        {
            var rangeItems = preview.Sections.SelectMany(s => s.Items)
                .Where(i => i.MinUnitPriceEur.HasValue && i.MaxUnitPriceEur.HasValue && i.MaxUnitPriceEur.Value > i.MinUnitPriceEur.Value).ToList();
            if (rangeItems.Any())
            {
                decimal totalMin = preview.Sections.SelectMany(s => s.Items).Sum(i => (i.MinUnitPriceEur ?? i.EffectiveUnitPriceEur) * i.Quantity);
                decimal totalMax = preview.Sections.SelectMany(s => s.Items).Sum(i => (i.MaxUnitPriceEur ?? i.EffectiveUnitPriceEur) * i.Quantity);
                preview.GrandTotalMinEur = totalMin;
                preview.GrandTotalMaxEur = totalMax;
                preview.GrandTotalRangeText = $"€ {totalMin:N0} – € {totalMax:N0}";
                preview.GrandTotalBgnRangeText = $"{Math.Round(totalMin * 1.95583m, 0):N0} – {Math.Round(totalMax * 1.95583m, 0):N0} лв.";
            }
        }

        return preview;
    }

    public async Task<ParsedOfferMarkdownDto> ParseMarkdownOfferAsync(string markdownContent)
    {
        var rawData = ParseMarkdownRaw(markdownContent);
        var parsedDto = new ParsedOfferMarkdownDto
        {
            ProjectTitle = rawData.ProjectTitle,
            ClientName = rawData.ClientName,
            ClientEmail = rawData.ClientEmail,
            ClientPhone = rawData.ClientPhone,
            SiteAddress = rawData.SiteAddress,
            AssignTo = rawData.AssignTo,
            AdminMarkupPercentage = rawData.AdminMarkupPercentage,
            Currency = rawData.Currency,
            Language = rawData.Language,
            ScopeDescription = rawData.ScopeDescription,
            GrandTotalRangeText = rawData.GrandTotalRangeText,
            GrandTotalBgnRangeText = rawData.GrandTotalBgnRangeText,
            GrandTotalMinEur = rawData.GrandTotalMinEur,
            GrandTotalMaxEur = rawData.GrandTotalMaxEur,
            AveragePricePerSqm = rawData.AveragePricePerSqm,
            Parameters = rawData.Parameters,
            InvestmentBreakdown = rawData.InvestmentBreakdown,
            EngineeringNotes = rawData.EngineeringNotes
        };

        var allSkus = await _context.ServiceSkus
            .Include(s => s.ServiceCategory)
            .AsNoTracking()
            .ToListAsync();

        var allCategories = await _context.ServiceCategories
            .AsNoTracking()
            .ToListAsync();

        int customSeq = 1;
        int globalItemIndex = 1;

        foreach (var rawSection in rawData.RawSections)
        {
            var matchedCategory = MatchCategory(allCategories, rawSection.CategoryName);

            var phase = new CustomOfferPhaseDto
            {
                PhaseTitle = rawSection.CategoryName,
                CategoryName = matchedCategory?.Name ?? rawSection.CategoryName,
                CategoryId = matchedCategory?.Id
            };

            foreach (var rawItem in rawSection.RawItems)
            {
                var matchedSku = MatchSku(allSkus, rawItem.SkuCode, rawItem.Title);

                decimal baseUnitPriceEur;
                if (rawItem.CustomUnitPriceEur.HasValue && rawItem.CustomUnitPriceEur.Value > 0)
                {
                    baseUnitPriceEur = rawItem.CustomUnitPriceEur.Value;
                }
                else if (matchedSku != null)
                {
                    baseUnitPriceEur = Math.Round(matchedSku.BasePrice / 1.95583m, 2);
                }
                else
                {
                    baseUnitPriceEur = 0m;
                    parsedDto.ValidationMessages.Add($"Перо '{rawItem.Title}' няма съответстващ SKU код и липсва единична цена.");
                }

                string normalizedUnit = NormalizeUnit(rawItem.Unit, matchedSku?.UnitType);
                string skuCode = matchedSku?.SkuCode 
                    ?? (!string.IsNullOrWhiteSpace(rawItem.SkuCode) ? rawItem.SkuCode : $"CUSTOM-{customSeq++:D2}");

                string itemDescription = !string.IsNullOrWhiteSpace(rawItem.PriceRangeText)
                    ? $"[RANGE: {rawItem.PriceRangeText}] " + (matchedSku?.Description ?? rawItem.Title)
                    : (matchedSku?.Description ?? rawItem.Title);

                phase.Items.Add(new CustomOfferItemDto
                {
                    Index = globalItemIndex++,
                    SkuCode = skuCode,
                    Title = !string.IsNullOrWhiteSpace(rawItem.Title) ? rawItem.Title : (matchedSku?.Name ?? skuCode),
                    Description = itemDescription,
                    Unit = normalizedUnit,
                    Quantity = rawItem.Quantity,
                    UnitPriceEur = baseUnitPriceEur,
                    PriceRangeText = rawItem.PriceRangeText,
                    MinPriceEur = rawItem.MinPriceEur,
                    MaxPriceEur = rawItem.MaxPriceEur,
                    SubItems = rawItem.SubItems != null ? new List<string>(rawItem.SubItems) : new List<string>()
                });
            }

            parsedDto.Phases.Add(phase);
        }

        return parsedDto;
    }

    private static ServiceSku? MatchSku(List<ServiceSku> allSkus, string? skuCode, string? title)
    {
        // 1. Exact match by SkuCode
        if (!string.IsNullOrWhiteSpace(skuCode))
        {
            var byCode = allSkus.FirstOrDefault(s => string.Equals(s.SkuCode, skuCode, StringComparison.OrdinalIgnoreCase));
            if (byCode != null) return byCode;
        }

        if (string.IsNullOrWhiteSpace(title)) return null;

        // 2. Check if title contains an SKU code (e.g. "Монтаж на контакт [ELEC-POINT-STD]" or "ELEC-POINT-STD: Окабеляване")
        var codeMatch = Regex.Match(title, @"\b([A-Z]{2,5}-[A-Z0-9-]+)\b");
        if (codeMatch.Success)
        {
            var byEmbeddedCode = allSkus.FirstOrDefault(s => string.Equals(s.SkuCode, codeMatch.Value, StringComparison.OrdinalIgnoreCase));
            if (byEmbeddedCode != null) return byEmbeddedCode;
        }

        // Clean title from numbering e.g. "1. Монтаж..." -> "Монтаж..."
        var cleanTitle = Regex.Replace(title, @"^\d+[\.\)]\s*", "").Trim();

        // 3. Exact match by Name or EnglishName
        var byName = allSkus.FirstOrDefault(s => 
            string.Equals(s.Name, cleanTitle, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(s.EnglishName) && string.Equals(s.EnglishName, cleanTitle, StringComparison.OrdinalIgnoreCase)));
        if (byName != null) return byName;

        // 4. Substring / Contains match (if title length >= 5)
        if (cleanTitle.Length >= 5)
        {
            var byContains = allSkus.FirstOrDefault(s =>
                s.Name.Contains(cleanTitle, StringComparison.OrdinalIgnoreCase) ||
                cleanTitle.Contains(s.Name, StringComparison.OrdinalIgnoreCase));
            if (byContains != null) return byContains;
        }

        return null;
    }

    private static ServiceCategory? MatchCategory(List<ServiceCategory> allCategories, string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName)) return null;

        var clean = CleanMarkdown(categoryName);

        var matched = allCategories.FirstOrDefault(c =>
            string.Equals(c.Name, clean, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(c.EnglishName) && string.Equals(c.EnglishName, clean, StringComparison.OrdinalIgnoreCase)) ||
            c.Name.Contains(clean, StringComparison.OrdinalIgnoreCase) ||
            clean.Contains(c.Name, StringComparison.OrdinalIgnoreCase));

        if (matched != null) return matched;

        // Keyword heuristics for Bulgarian trade categories
        var lower = clean.ToLowerInvariant();
        if (lower.Contains("електро") || lower.Contains("ел.") || lower.Contains("осветл"))
        {
            return allCategories.FirstOrDefault(c => c.Name.Contains("Електро", StringComparison.OrdinalIgnoreCase));
        }
        if (lower.Contains("вик") || lower.Contains("водопровод") || lower.Contains("канал") || lower.Contains("баня"))
        {
            return allCategories.FirstOrDefault(c => c.Name.Contains("ВиК", StringComparison.OrdinalIgnoreCase));
        }
        if (lower.Contains("боя") || lower.Contains("латекс") || lower.Contains("боядис"))
        {
            return allCategories.FirstOrDefault(c => c.Name.Contains("Боя", StringComparison.OrdinalIgnoreCase));
        }
        if (lower.Contains("шпакл") || lower.Contains("гипс") || lower.Contains("картон") || lower.Contains("сухо"))
        {
            return allCategories.FirstOrDefault(c => c.Name.Contains("Шпакл", StringComparison.OrdinalIgnoreCase) || c.Name.Contains("Сухо", StringComparison.OrdinalIgnoreCase));
        }
        if (lower.Contains("кърт") || lower.Contains("чист") || lower.Contains("извоз") || lower.Contains("демонт") || lower.Contains("отпад"))
        {
            return allCategories.FirstOrDefault(c => c.Name.Contains("Кърт", StringComparison.OrdinalIgnoreCase) || c.Name.Contains("Извоз", StringComparison.OrdinalIgnoreCase));
        }
        if (lower.Contains("настилк") || lower.Contains("плоч") || lower.Contains("фаянс") || lower.Contains("теракот") || lower.Contains("паркет") || lower.Contains("ламинат"))
        {
            return allCategories.FirstOrDefault(c => c.Name.Contains("Настилк", StringComparison.OrdinalIgnoreCase) || c.Name.Contains("Под", StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    private static string NormalizeUnit(string? rawUnit, string? skuUnit)
    {
        var unit = CleanMarkdown(rawUnit ?? skuUnit ?? "бр.").ToLowerInvariant();

        if (unit.Contains("%"))
            return "к-т";

        if (unit == "м2" || unit == "кв.м" || unit == "кв.м." || unit == "кв. метра" || unit == "кв.м" || unit == "sqm" || unit == "m2" || unit == "м²")
            return "м²";

        if (unit == "м" || unit == "л.м" || unit == "л.м." || unit == "лм" || unit == "m" || unit == "meter" || unit == "meters")
            return "м";

        if (unit == "бр" || unit == "бр." || unit == "брой" || unit == "броя" || unit == "pcs" || unit == "pc" || unit == "точка" || unit == "излаз")
            return "бр.";

        if (unit == "к-т" || unit == "к-кт" || unit == "комплект" || unit == "set" || unit == "пакет")
            return "к-т";

        if (unit == "кг" || unit == "кг." || unit == "kg")
            return "кг";

        if (unit == "т" || unit == "тон" || unit == "t")
            return "т";

        return string.IsNullOrWhiteSpace(rawUnit) ? (skuUnit ?? "бр.") : rawUnit.Trim();
    }

    public static RawParsedMarkdown ParseMarkdownRaw(string markdownContent)
    {
        var result = new RawParsedMarkdown();
        if (string.IsNullOrWhiteSpace(markdownContent)) return result;

        var lines = markdownContent.Replace("\r\n", "\n").Split('\n');

        if (IsStructuredOffer(lines))
        {
            ParseStructuredOffer(markdownContent, lines, result);
            return result;
        }

        int lineIndex = 0;

        // 1. Parse YAML Frontmatter if present
        if (lines.Length > 0 && lines[0].Trim() == "---")
        {
            lineIndex++;
            while (lineIndex < lines.Length)
            {
                var line = lines[lineIndex].Trim();
                if (line == "---")
                {
                    lineIndex++;
                    break;
                }

                if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                {
                    ParseMetadataKeyValue(line, result);
                }
                lineIndex++;
            }
        }

        // 2. Parse Body (Headings, Free-text metadata, and Tables)
        RawSection? currentSection = null;
        var scopeLines = new List<string>();
        bool beforeFirstSection = true;

        while (lineIndex < lines.Length)
        {
            var rawLine = lines[lineIndex];
            var line = rawLine.Trim();

            // Check if top free-text contains metadata (e.g. "Клиент: Иван Иванов" or "**Обект:** София")
            if (beforeFirstSection && !line.StartsWith("#") && !line.StartsWith("|") && line.Contains(':'))
            {
                var parts = line.Split('•');
                foreach (var part in parts)
                {
                    if (part.Contains(':'))
                    {
                        ParseMetadataKeyValue(part.Trim(), result);
                    }
                }
                lineIndex++;
                continue;
            }

            // Heading 1: Project Title / Scope
            if (line.StartsWith("# ") && !line.StartsWith("## "))
            {
                var h1 = CleanMarkdown(line.Substring(2).Trim());
                if (string.IsNullOrWhiteSpace(result.ProjectTitle) || result.ProjectTitle == "Официална Оферта за СМР")
                {
                    result.ProjectTitle = h1;
                }
                lineIndex++;
                continue;
            }

            // Heading 2 or 3: Section / Category
            if (line.StartsWith("## ") || line.StartsWith("### "))
            {
                beforeFirstSection = false;
                var sectionTitle = CleanMarkdown(line.TrimStart('#', ' ').Trim());
                if (sectionTitle.StartsWith("Раздел:", StringComparison.OrdinalIgnoreCase))
                {
                    sectionTitle = sectionTitle.Substring("Раздел:".Length).Trim();
                }
                else if (sectionTitle.StartsWith("Раздел ", StringComparison.OrdinalIgnoreCase))
                {
                    sectionTitle = sectionTitle.Substring("Раздел ".Length).Trim();
                }
                else if (sectionTitle.StartsWith("Фаза:", StringComparison.OrdinalIgnoreCase))
                {
                    sectionTitle = sectionTitle.Substring("Фаза:".Length).Trim();
                }
                else if (sectionTitle.StartsWith("Етап:", StringComparison.OrdinalIgnoreCase))
                {
                    sectionTitle = sectionTitle.Substring("Етап:".Length).Trim();
                }

                currentSection = new RawSection { CategoryName = sectionTitle };
                result.RawSections.Add(currentSection);
                lineIndex++;
                continue;
            }

            // Table Row
            if (line.StartsWith("|"))
            {
                beforeFirstSection = false;
                if (currentSection == null)
                {
                    currentSection = new RawSection { CategoryName = "Общи СМР дейности" };
                    result.RawSections.Add(currentSection);
                }

                // Collect contiguous table lines
                var tableLines = new List<string>();
                while (lineIndex < lines.Length && lines[lineIndex].Trim().StartsWith("|"))
                {
                    tableLines.Add(lines[lineIndex].Trim());
                    lineIndex++;
                }

                ParseTableIntoSection(tableLines, currentSection, result.Currency);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(line))
            {
                scopeLines.Add(line);
            }

            lineIndex++;
        }

        result.ScopeDescription = string.Join("\n\n", scopeLines).Trim();
        if (string.IsNullOrWhiteSpace(result.ProjectTitle))
        {
            result.ProjectTitle = "Официална Оферта за СМР";
        }
        if (string.IsNullOrWhiteSpace(result.ClientName))
        {
            result.ClientName = string.Empty;
        }

        // Post-process sections:
        // Check if there is a summary/recapitulation section (e.g. "Труд 48%, Чернови 27%, Чистови 25%")
        // AND another section with detailed activities (e.g. 9 СМР дейности).
        // If so, keep the detailed section and exclude the summary section so the total is not doubled!
        if (result.RawSections.Count > 1)
        {
            var summarySections = result.RawSections.Where(s => 
                s.RawItems.All(i => i.IsPercentageOrBudgetGroup) ||
                s.CategoryName.Contains("бюджет", StringComparison.OrdinalIgnoreCase) ||
                s.CategoryName.Contains("рекапитулация", StringComparison.OrdinalIgnoreCase) ||
                s.CategoryName.Contains("структура", StringComparison.OrdinalIgnoreCase) ||
                s.CategoryName.Contains("обобщение", StringComparison.OrdinalIgnoreCase)).ToList();

            var detailedSections = result.RawSections.Where(s => !summarySections.Contains(s) && s.RawItems.Any()).ToList();

            if (detailedSections.Any())
            {
                result.RawSections = detailedSections;
            }
        }

        return result;
    }

    private static bool IsStructuredOffer(string[] lines)
    {
        return lines.Any(l => l.Contains("ОБЩИ ДАННИ ЗА ОБЕКТА") || 
                              l.Contains("СТРУКТУРА НА ИНВЕСТИЦИЯТА") || 
                              l.Contains("ОБОБЩЕНА ТАБЛИЦА НА ОФЕРТАТА"));
    }

    private static void ParseStructuredOffer(string markdownContent, string[] lines, RawParsedMarkdown result)
    {
        var h1 = lines.FirstOrDefault(l => l.StartsWith("# ") && !l.StartsWith("## "));
        if (!string.IsNullOrWhiteSpace(h1))
        {
            result.ProjectTitle = CleanMarkdown(h1.Substring(2).Trim());
        }

        var h2 = lines.FirstOrDefault(l => l.StartsWith("## ") && !l.StartsWith("### "));
        if (!string.IsNullOrWhiteSpace(h2) && !h2.Contains("СТРУКТУРА") && !h2.Contains("РАЗДЕЛ"))
        {
            var sub = CleanMarkdown(h2.Substring(3).Trim());
            if (!string.IsNullOrWhiteSpace(sub))
            {
                result.ProjectTitle = sub;
            }
        }

        int lineIndex = 0;
        if (lines.Length > 0 && lines[0].Trim() == "---")
        {
            lineIndex++;
            while (lineIndex < lines.Length)
            {
                var line = lines[lineIndex].Trim();
                if (line == "---")
                {
                    lineIndex++;
                    break;
                }
                if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                {
                    ParseMetadataKeyValue(line, result);
                }
                lineIndex++;
            }
        }

        int s1Start = FindLineIndex(lines, l => l.Contains("1. ОБЩИ ДАННИ"));
        int s2Start = FindLineIndex(lines, l => l.Contains("2. СТРУКТУРА НА ИНВЕСТИЦИЯТА"));
        int s3Start = FindLineIndex(lines, l => l.Contains("3. ДЕТАЙЛНА КОЛИЧЕСТВЕНО"));
        int s4Start = FindLineIndex(lines, l => l.Contains("4. ОБОБЩЕНА ТАБЛИЦА"));
        int s5Start = FindLineIndex(lines, l => l.Contains("5. ИНЖЕНЕРНИ ПРЕПОРЪКИ"));

        // Check any metadata before Section 1
        int headerLimit = s1Start >= 0 ? s1Start : lines.Length;
        for (int i = lineIndex; i < headerLimit; i++)
        {
            var line = lines[i].Trim();
            if (!line.StartsWith("#") && !line.StartsWith("|") && line.Contains(':'))
            {
                var parts = line.Split('•');
                foreach (var part in parts)
                {
                    if (part.Contains(':'))
                    {
                        ParseMetadataKeyValue(part.Trim(), result);
                    }
                }
            }
        }

        // Section 1: ОБЩИ ДАННИ
        if (s1Start >= 0)
        {
            int s1End = s2Start > s1Start ? s2Start : lines.Length;
            for (int i = s1Start; i < s1End; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("-") || line.StartsWith("*"))
                {
                    var match = Regex.Match(line, @"^[-*]\s*\*\*([^*:]+)(?:\*\*:|:\*\*)\s*(.*)$");
                    if (!match.Success)
                    {
                        match = Regex.Match(line, @"^[-*]\s*([^:]+):\s*(.*)$");
                    }
                    if (match.Success)
                    {
                        var key = match.Groups[1].Value.Trim();
                        var val = match.Groups[2].Value.Trim();
                        var cleanVal = CleanMarkdown(val);

                        if (key.Contains("Местоположение") || key.Contains("Обект") || key.Contains("Адрес"))
                        {
                            result.Parameters.Location = cleanVal;
                            result.SiteAddress = cleanVal;
                        }
                        else if (key.Contains("Клиент") || key.Contains("Възложител") || key.Contains("Собственик") || key.Contains("Инвеститор") || key.Contains("Получател") || key.Contains("Заявител") || key.Contains("Поръчител") || key.Contains("Изготвено") || key.Contains("Подготвено") || key.Contains("Client") || key.Contains("Owner") || key.Contains("Prepared"))
                        {
                            if (!cleanVal.Equals("Възложител", StringComparison.OrdinalIgnoreCase) &&
                                !cleanVal.Equals("Уважаеми Клиент", StringComparison.OrdinalIgnoreCase) &&
                                !cleanVal.Equals("Valued Client", StringComparison.OrdinalIgnoreCase))
                            {
                                result.ClientName = cleanVal;
                            }
                            else if (string.IsNullOrWhiteSpace(result.ClientName))
                            {
                                result.ClientName = cleanVal;
                            }
                        }
                        else if (key.Contains("Възраст") || key.Contains("състояние"))
                        {
                            result.Parameters.PropertyCondition = cleanVal;
                        }
                        else if (key.Contains("площ"))
                        {
                            result.Parameters.TotalArea = cleanVal;
                        }
                        else if (key.Contains("Срок"))
                        {
                            result.Parameters.ExecutionTimeline = cleanVal;
                        }
                        else if (key.Contains("Ниво"))
                        {
                            result.Parameters.QualityLevel = cleanVal;
                        }
                        else if (key.Contains("Гаранция"))
                        {
                            result.Parameters.Warranty = cleanVal;
                        }
                        else if (key.Contains("плащане"))
                        {
                            result.Parameters.PaymentTerms = cleanVal;
                        }
                        else
                        {
                            result.Parameters.AdditionalDetails[key] = cleanVal;
                        }
                    }
                }
            }
        }

        // Section 2: СТРУКТУРА НА ИНВЕСТИЦИЯТА
        if (s2Start >= 0)
        {
            int s2End = s3Start > s2Start ? s3Start : (s4Start > s2Start ? s4Start : lines.Length);
            var tableLines = new List<string>();
            for (int i = s2Start; i < s2End; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("|"))
                {
                    tableLines.Add(line);
                }
                else if (line.Contains("€") && line.Contains("–") && string.IsNullOrEmpty(result.GrandTotalRangeText))
                {
                    var m = Regex.Match(line, @"(?:€|EUR)?\s*([0-9\s]+)\s*–\s*(?:€|EUR)?\s*([0-9\s]+)");
                    if (m.Success)
                    {
                        var min = ParseSingleNumber(m.Groups[1].Value);
                        var max = ParseSingleNumber(m.Groups[2].Value);
                        if (min.HasValue && max.HasValue)
                        {
                            result.GrandTotalMinEur = min.Value;
                            result.GrandTotalMaxEur = max.Value;
                            result.GrandTotalRangeText = $"€ {min.Value:N0} – € {max.Value:N0}";
                            result.GrandTotalBgnRangeText = $"{Math.Round(min.Value * 1.95583m, 0):N0} – {Math.Round(max.Value * 1.95583m, 0):N0} лв.";
                        }
                    }
                }
            }

            if (tableLines.Count >= 2)
            {
                for (int r = 1; r < tableLines.Count; r++)
                {
                    if (tableLines[r].Contains("---")) continue;
                    var cells = SplitTableRow(tableLines[r]);
                    if (cells.Length >= 4)
                    {
                        string compName = CleanMarkdown(cells[0]);
                        string share = cells.Length > 1 ? CleanMarkdown(cells[1]) : "";
                        string eurRange = cells.Length > 2 ? CleanMarkdown(cells[2]) : "";
                        string bgnRange = cells.Length > 3 ? CleanMarkdown(cells[3]) : "";
                        string desc = cells.Length > 4 ? CleanMarkdown(cells[4]) : "";

                        result.InvestmentBreakdown.Add(new InvestmentComponentDto
                        {
                            Name = compName,
                            SharePercentage = share,
                            AmountEurRange = eurRange,
                            AmountBgnRange = bgnRange,
                            Description = desc
                        });

                        if (compName.Contains("ОБЩО"))
                        {
                            if (string.IsNullOrEmpty(result.GrandTotalRangeText))
                            {
                                result.GrandTotalRangeText = eurRange;
                                result.GrandTotalBgnRangeText = bgnRange;
                            }
                            if (desc.Contains("Средна цена"))
                            {
                                result.AveragePricePerSqm = desc;
                            }
                        }
                    }
                }
            }
        }

        // Section 3: ДЕТАЙЛНА КОЛИЧЕСТВЕНО-СТОЙНОСТНА СМЕТКА (Sub-activities & Callouts)
        var subActivitiesBySectionIndex = new Dictionary<int, List<string>>();
        var subActivitiesBySectionTitle = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var sectionVariantNotes = new Dictionary<int, List<string>>();

        if (s3Start >= 0)
        {
            int s3End = s4Start > s3Start ? s4Start : (s5Start > s3Start ? s5Start : lines.Length);
            int currentSectionIndex = -1;
            string currentSectionTitle = "";
            var currentSubItems = new List<string>();

            Action saveCurrentSectionItems = () =>
            {
                if (currentSectionIndex >= 0)
                {
                    if (!currentSubItems.Any() && sectionVariantNotes.TryGetValue(currentSectionIndex, out var variants) && variants.Any())
                    {
                        currentSubItems.AddRange(variants);
                    }

                    if (currentSubItems.Any())
                    {
                        subActivitiesBySectionIndex[currentSectionIndex] = new List<string>(currentSubItems);
                        if (!string.IsNullOrWhiteSpace(currentSectionTitle))
                            subActivitiesBySectionTitle[currentSectionTitle] = new List<string>(currentSubItems);
                    }
                    currentSubItems.Clear();
                }
            };

            for (int i = s3Start; i < s3End; i++)
            {
                var rawLine = lines[i];
                var line = rawLine.Trim();
                if (line.StartsWith("#### РАЗДЕЛ", StringComparison.OrdinalIgnoreCase) || line.StartsWith("#### Раздел", StringComparison.OrdinalIgnoreCase))
                {
                    saveCurrentSectionItems();

                    var secMatch = Regex.Match(line, @"РАЗДЕЛ\s*(\d+)[:\s]*(.*)$", RegexOptions.IgnoreCase);
                    if (secMatch.Success)
                    {
                        int.TryParse(secMatch.Groups[1].Value, out currentSectionIndex);
                        currentSectionTitle = CleanMarkdown(secMatch.Groups[2].Value);
                    }
                    continue;
                }

                if (Regex.IsMatch(line, @"^\d+[\.\)]\s*\*\*"))
                {
                    currentSubItems.Add(CleanMarkdown(line));
                    continue;
                }

                // Nested bullet points or continuation lines for the current sub-item
                if (currentSubItems.Any() &&
                    (rawLine.StartsWith("   ") || rawLine.StartsWith("\t") || rawLine.StartsWith("  -") || rawLine.StartsWith("  *") ||
                     ((line.StartsWith("- ") || line.StartsWith("* ")) && !line.Contains("Общ бюджет") && !line.Contains("Труд (") && !line.Contains("Чернови") && !line.Contains("Чистови") && !line.Contains("Транспорт ("))))
                {
                    var cleanSub = CleanMarkdown(line.TrimStart('-', '*', ' ').Trim());
                    if (!string.IsNullOrWhiteSpace(cleanSub))
                    {
                        int lastIdx = currentSubItems.Count - 1;
                        if (currentSubItems[lastIdx].EndsWith(":"))
                        {
                            currentSubItems[lastIdx] += " " + cleanSub;
                        }
                        else
                        {
                            currentSubItems[lastIdx] += "; " + cleanSub;
                        }
                    }
                    continue;
                }

                if (line.StartsWith("> [!NOTE]") || line.StartsWith("> [!TIP]") || line.StartsWith("> [!IMPORTANT]"))
                {
                    string noteType = line.Contains("TIP") ? "tip" : "note";
                    var noteTitle = "";
                    var noteLines = new List<string>();
                    i++;
                    while (i < s3End && lines[i].Trim().StartsWith(">"))
                    {
                        var nLine = lines[i].Trim().TrimStart('>', ' ').Trim();
                        if (string.IsNullOrEmpty(noteTitle) && nLine.StartsWith("**"))
                        {
                            noteTitle = CleanMarkdown(nLine);
                        }
                        else
                        {
                            noteLines.Add(CleanMarkdown(nLine));
                        }
                        i++;
                    }
                    i--; // Step back so the outer for-loop's i++ processes the first non-blockquote line

                    string costEst = "";
                    var costLine = noteLines.FirstOrDefault(l => l.Contains("Стойност"));
                    if (costLine != null)
                    {
                        var bMatch = Regex.Match(costLine, @"\*\*([^*]+)\*\*");
                        if (bMatch.Success)
                        {
                            costEst = bMatch.Groups[1].Value.Trim();
                        }
                        else
                        {
                            var cMatch = Regex.Match(costLine, @"Стойност[^:]*:\s*(.*)$");
                            if (cMatch.Success) costEst = cMatch.Groups[1].Value.Trim();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(noteTitle))
                    {
                        if (currentSectionIndex >= 0)
                        {
                            if (!sectionVariantNotes.TryGetValue(currentSectionIndex, out var vList))
                            {
                                vList = new List<string>();
                                sectionVariantNotes[currentSectionIndex] = vList;
                            }
                            string variantItem = !string.IsNullOrWhiteSpace(costEst) ? $"{noteTitle} – {costEst}" : noteTitle;
                            vList.Add(variantItem);
                        }
                    }

                    result.EngineeringNotes.Add(new EngineeringNoteDto
                    {
                        Title = string.IsNullOrWhiteSpace(noteTitle) ? "Инженерна бележка" : noteTitle,
                        Content = string.Join("\n", noteLines),
                        CostEstimate = costEst,
                        Type = noteType
                    });
                }
            }

            saveCurrentSectionItems();
        }

        // Section 4: ОБОБЩЕНА ТАБЛИЦА НА ОФЕРТАТА (The 9 СМР categories)
        if (s4Start >= 0)
        {
            int s4End = s5Start > s4Start ? s5Start : lines.Length;
            var tableLines = new List<string>();
            for (int i = s4Start; i < s4End; i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("|"))
                {
                    tableLines.Add(line);
                }
            }

            if (tableLines.Count >= 2)
            {
                var mainSection = new RawSection { CategoryName = "Количествено-Стойностна Сметка (КСС)" };
                result.RawSections.Add(mainSection);

                for (int r = 1; r < tableLines.Count; r++)
                {
                    if (tableLines[r].Contains("---")) continue;
                    var cells = SplitTableRow(tableLines[r]);
                    if (cells.Length < 3) continue;

                    string firstCell = CleanMarkdown(cells[0]);
                    string secTitle = CleanMarkdown(cells[1]);
                    string budgetCell = CleanMarkdown(cells[2]);

                    if (IsSummaryOrTotalRow(firstCell, secTitle, cells)) continue;
                    if (string.IsNullOrWhiteSpace(secTitle)) continue;

                    int.TryParse(Regex.Replace(firstCell, @"\D", ""), out int secIdx);

                    var range = ParseNumberAndRange(budgetCell, false);

                    var subItems = new List<string>();
                    if (secIdx > 0 && subActivitiesBySectionIndex.TryGetValue(secIdx, out var byIdx))
                    {
                        subItems.AddRange(byIdx);
                    }
                    else
                    {
                        var matchingTitleKvp = subActivitiesBySectionTitle.FirstOrDefault(kvp => 
                            kvp.Key.Contains(secTitle, StringComparison.OrdinalIgnoreCase) || 
                            secTitle.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase));
                        if (matchingTitleKvp.Value != null)
                        {
                            subItems.AddRange(matchingTitleKvp.Value);
                        }
                    }

                    mainSection.RawItems.Add(new RawItem
                    {
                        SkuCode = $"KSS-{secIdx:D2}",
                        Title = secTitle,
                        Unit = "к-т",
                        Quantity = 1.0m,
                        CustomUnitPriceEur = range.Avg,
                        MinPriceEur = range.Min,
                        MaxPriceEur = range.Max,
                        PriceRangeText = range.RangeText ?? budgetCell,
                        SubItems = subItems
                    });
                }
            }
        }

        // Section 5: ИНЖЕНЕРНИ ПРЕПОРЪКИ
        if (s5Start >= 0)
        {
            for (int i = s5Start; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                var m = Regex.Match(line, @"^\d+[\.\)]\s*\*\*([^*]+?)(?:\*\*:|:\*\*|\*\*)\s*(.*)$");
                if (!m.Success)
                {
                    m = Regex.Match(line, @"^\d+[\.\)]\s*([^:]+):\s*(.*)$");
                }
                if (m.Success)
                {
                    var title = m.Groups[1].Value.Trim();
                    var contentLines = new List<string>();
                    if (!string.IsNullOrWhiteSpace(m.Groups[2].Value))
                    {
                        contentLines.Add(CleanMarkdown(m.Groups[2].Value));
                    }

                    int j = i + 1;
                    while (j < lines.Length && !Regex.IsMatch(lines[j].Trim(), @"^\d+[\.\)]\s*\*\*") && !Regex.IsMatch(lines[j].Trim(), @"^\d+[\.\)]\s*[^:]+:") && !lines[j].Trim().StartsWith("#"))
                    {
                        if (!string.IsNullOrWhiteSpace(lines[j].Trim()))
                        {
                            contentLines.Add(CleanMarkdown(lines[j].Trim()));
                        }
                        j++;
                    }
                    i = j - 1;

                    result.EngineeringNotes.Add(new EngineeringNoteDto
                    {
                        Title = title,
                        Content = string.Join("\n", contentLines),
                        Type = "recommendation"
                    });
                }
            }
        }

        result.AdminMarkupPercentage = 0m;
        result.ScopeDescription = markdownContent;
    }

    private static int FindLineIndex(string[] lines, Func<string, bool> predicate)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (predicate(lines[i])) return i;
        }
        return -1;
    }

    private static void ParseMetadataKeyValue(string line, RawParsedMarkdown result)
    {
        var clean = CleanMarkdown(line).TrimStart('•', '-', '*').Trim();
        var colonIndex = clean.IndexOf(':');
        if (colonIndex <= 0) return;

        var rawKey = clean.Substring(0, colonIndex).Trim();
        var value = clean.Substring(colonIndex + 1).Trim().Trim('"', '\'');

        // Remove inline comment
        if (value.Contains('#'))
        {
            value = value.Substring(0, value.IndexOf('#')).Trim().Trim('"', '\'');
        }

        var key = Regex.Replace(rawKey.ToLowerInvariant(), @"[^a-zа-я0-9]", "");

        switch (key)
        {
            case "projecttitle":
            case "title":
            case "проект":
            case "заглавие":
            case "обектнаименование":
                result.ProjectTitle = value;
                break;
            case "clientname":
            case "client":
            case "клиент":
            case "възложител":
            case "собственик":
            case "собственика":
            case "инвеститор":
            case "получател":
            case "заявител":
            case "поръчител":
            case "изготвеноза":
            case "изготвено":
            case "подготвеноза":
            case "подготвено":
            case "preparedfor":
            case "prepared":
            case "owner":
            case "customer":
                if (!value.Equals("Възложител", StringComparison.OrdinalIgnoreCase) &&
                    !value.Equals("Уважаеми Клиент", StringComparison.OrdinalIgnoreCase) &&
                    !value.Equals("Valued Client", StringComparison.OrdinalIgnoreCase))
                {
                    result.ClientName = value;
                }
                else if (string.IsNullOrWhiteSpace(result.ClientName))
                {
                    result.ClientName = value;
                }
                break;
            case "clientemail":
            case "email":
            case "имейл":
            case "елпоща":
            case "поща":
                result.ClientEmail = value;
                break;
            case "clientphone":
            case "phone":
            case "телефон":
            case "тел":
                result.ClientPhone = value;
                break;
            case "siteaddress":
            case "address":
            case "обект":
            case "адрес":
            case "локация":
            case "местоположение":
                result.SiteAddress = value;
                break;
            case "assignto":
            case "assign":
            case "назначи":
            case "назначаване":
                result.AssignTo = value;
                break;
            case "currency":
            case "валута":
                result.Currency = value.ToUpperInvariant();
                break;
            case "adminmarkuppercentage":
            case "markup":
            case "markuppercent":
            case "надценка":
            case "марж":
                var cleanMarkup = Regex.Replace(value, @"[^\d.,]", "");
                if (decimal.TryParse(cleanMarkup.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var markup))
                    result.AdminMarkupPercentage = markup;
                break;
            case "language":
            case "език":
                result.Language = value.ToLowerInvariant();
                break;
        }
    }

    private static void ParseTableIntoSection(List<string> tableLines, RawSection section, string defaultCurrency)
    {
        if (tableLines.Count < 2) return;

        // Line 0: Header
        var headerCells = SplitTableRow(tableLines[0]);
        int indexCol = -1;
        int codeCol = -1;
        int titleCol = -1;
        int percentCol = -1;
        int unitCol = -1;
        int qtyCol = -1;
        int activityTotalEurCol = -1;
        int activityTotalBgnCol = -1;
        int priceEurCol = -1;
        int priceBgnCol = -1;
        int generalPriceCol = -1;
        int totalCol = -1;

        for (int i = 0; i < headerCells.Length; i++)
        {
            var h = CleanMarkdown(headerCells[i]).ToLowerInvariant();

            // Index column (№, No, #, Поз)
            if (h == "№" || h == "no" || h == "#" || h == "поз" || h == "поз." || h == "позиция" || h == "num" || h == "item")
            {
                indexCol = i;
            }
            // Code column: only if explicitly mentions code or sku!
            else if (h.Contains("код") || h.Contains("sku") || h.Contains("code"))
            {
                codeCol = i;
            }
            // Percent / Share column
            else if (h.Contains("%") || h.Contains("дял") || h.Contains("процент"))
            {
                percentCol = i;
            }
            // Title / Description column:
            else if (h.Contains("дейност") || h.Contains("описание") || h.Contains("перо") || h.Contains("вид смр") || 
                     h.Contains("смр") || h.Contains("процес") || h.Contains("task") || h.Contains("description") || 
                     h.Contains("title") || h.Contains("наименование") || h.Contains("работи") || h.Contains("етап") ||
                     h.Contains("разходно"))
            {
                if (titleCol == -1) titleCol = i;
            }
            // Unit column
            else if (h.Contains("мярка") || h.Contains("unit") || h.Contains("м-ка") || h.Contains("ед. мярка") || h.Contains("дименсия"))
            {
                unitCol = i;
            }
            // Quantity column
            else if (h.Contains("колич") || h.Contains("qty") || h.Contains("кол.") || h.Contains("кол-во") || h.Contains("к-во") || h.Contains("обем"))
            {
                qtyCol = i;
            }
            else
            {
                bool isEur = h.Contains("eur") || h.Contains("€") || h.Contains("евро");
                bool isBgn = h.Contains("bgn") || h.Contains("лв") || h.Contains("лева");
                bool isTotalWord = h.Contains("общ") || h.Contains("total") || h.Contains("индикатив") || h.Contains("крайн") || h.Contains("сума");
                bool isSubComponent = h.Contains("труд") || h.Contains("чернов") || h.Contains("чистов") || h.Contains("материал");

                if (isTotalWord && isEur)
                {
                    if (activityTotalEurCol == -1) activityTotalEurCol = i;
                }
                else if (isTotalWord && isBgn)
                {
                    if (activityTotalBgnCol == -1) activityTotalBgnCol = i;
                }
                else if (isEur && !isSubComponent)
                {
                    if (priceEurCol == -1) priceEurCol = i;
                }
                else if (isBgn && !isSubComponent)
                {
                    if (priceBgnCol == -1) priceBgnCol = i;
                }
                else if (h.Contains("ед. цена") || h.Contains("ед.цена") || h.Contains("цена/м") || h.Contains("unit price") || h.Contains("цена") || h.Contains("price"))
                {
                    if (generalPriceCol == -1) generalPriceCol = i;
                }
                else if (isTotalWord || h.Contains("стойност") || h.Contains("amount"))
                {
                    if (totalCol == -1) totalCol = i;
                }
            }
        }

        // Resolve titleCol fallback
        if (titleCol == -1)
        {
            if (indexCol == 0 && headerCells.Length >= 2) titleCol = 1;
            else titleCol = 0;
        }

        int activePriceCol = -1;
        bool activePriceIsBgn = false;

        if (activityTotalEurCol >= 0)
        {
            activePriceCol = activityTotalEurCol;
            activePriceIsBgn = false;
        }
        else if (priceEurCol >= 0)
        {
            activePriceCol = priceEurCol;
            activePriceIsBgn = false;
        }
        else if (activityTotalBgnCol >= 0)
        {
            activePriceCol = activityTotalBgnCol;
            activePriceIsBgn = true;
        }
        else if (generalPriceCol >= 0)
        {
            activePriceCol = generalPriceCol;
            activePriceIsBgn = defaultCurrency.Equals("BGN", StringComparison.OrdinalIgnoreCase);
        }
        else if (priceBgnCol >= 0)
        {
            activePriceCol = priceBgnCol;
            activePriceIsBgn = true;
        }
        else if (totalCol >= 0)
        {
            activePriceCol = totalCol;
            activePriceIsBgn = defaultCurrency.Equals("BGN", StringComparison.OrdinalIgnoreCase);
        }

        for (int r = 1; r < tableLines.Count; r++)
        {
            var rowText = tableLines[r];
            if (rowText.Contains("---")) continue;

            var cells = SplitTableRow(rowText);
            if (cells.Length == 0) continue;
            if (cells.All(c => string.IsNullOrWhiteSpace(CleanMarkdown(c)))) continue;

            string skuCode = (codeCol >= 0 && codeCol < cells.Length) ? CleanMarkdown(cells[codeCol]) : string.Empty;
            string title = (titleCol >= 0 && titleCol < cells.Length) ? CleanMarkdown(cells[titleCol]) : string.Empty;
            string unit = (unitCol >= 0 && unitCol < cells.Length) ? CleanMarkdown(cells[unitCol]) : string.Empty;
            string qtyStr = (qtyCol >= 0 && qtyCol < cells.Length) ? CleanMarkdown(cells[qtyCol]) : string.Empty;

            if (IsSummaryOrTotalRow(skuCode, title, cells))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(skuCode) && string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            bool isBudgetGroup = false;
            if (percentCol >= 0 && percentCol < cells.Length && cells[percentCol].Contains('%'))
            {
                isBudgetGroup = true;
            }
            else if (cells.Any(c => c.Trim().EndsWith("%")))
            {
                isBudgetGroup = true;
            }

            decimal? customPriceEur = null;
            decimal? minPriceEur = null;
            decimal? maxPriceEur = null;
            string? priceRangeText = null;

            if (activePriceCol >= 0 && activePriceCol < cells.Length)
            {
                var priceCellStr = CleanMarkdown(cells[activePriceCol]);
                var pr = ParseNumberAndRange(priceCellStr, activePriceIsBgn);
                customPriceEur = pr.Avg;
                minPriceEur = pr.Min;
                maxPriceEur = pr.Max;
                priceRangeText = pr.RangeText;
            }

            if (!customPriceEur.HasValue && activePriceCol == -1)
            {
                for (int ci = 0; ci < cells.Length; ci++)
                {
                    if (ci == titleCol || ci == indexCol || ci == codeCol || ci == percentCol || ci == qtyCol || ci == unitCol) continue;
                    var cellContent = CleanMarkdown(cells[ci]);
                    var pr = ParseNumberAndRange(cellContent, defaultCurrency.Equals("BGN", StringComparison.OrdinalIgnoreCase));
                    if (pr.Avg.HasValue && pr.Avg.Value > 0)
                    {
                        customPriceEur = pr.Avg;
                        minPriceEur = pr.Min;
                        maxPriceEur = pr.Max;
                        priceRangeText = pr.RangeText;
                        break;
                    }
                }
            }

            decimal qty = 1.0m;
            if (qtyCol >= 0 && qtyCol < cells.Length && !string.IsNullOrWhiteSpace(qtyStr))
            {
                var parsedQty = ParseNumberOrRange(qtyStr, false);
                if (parsedQty.HasValue && parsedQty.Value > 0)
                {
                    qty = parsedQty.Value;
                }
            }

            if (string.IsNullOrWhiteSpace(unit))
            {
                unit = "к-т";
            }

            section.RawItems.Add(new RawItem
            {
                SkuCode = skuCode,
                Title = title,
                Unit = unit,
                Quantity = qty,
                CustomUnitPriceEur = customPriceEur,
                MinPriceEur = minPriceEur,
                MaxPriceEur = maxPriceEur,
                PriceRangeText = priceRangeText,
                IsPercentageOrBudgetGroup = isBudgetGroup
            });
        }
    }

    public static (decimal? Avg, decimal? Min, decimal? Max, string? RangeText) ParseNumberAndRange(string? input, bool convertBgnToEur = false)
    {
        if (string.IsNullOrWhiteSpace(input)) return (null, null, null, null);

        var clean = CleanMarkdown(input).Trim();

        bool hasEur = clean.Contains("€") || clean.Contains("eur", StringComparison.OrdinalIgnoreCase);
        bool hasBgn = clean.Contains("лв", StringComparison.OrdinalIgnoreCase) || clean.Contains("bgn", StringComparison.OrdinalIgnoreCase);

        string[] separators = { " – ", " — ", " - ", "–", "—", " до ", " to " };
        string? matchedSep = separators.FirstOrDefault(s => clean.Contains(s));

        if (matchedSep != null)
        {
            var parts = clean.Split(new[] { matchedSep }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var p1 = ParseSingleNumber(parts[0]);
                var p2 = ParseSingleNumber(parts[1]);

                if (p1.HasValue && p2.HasValue)
                {
                    decimal min = Math.Min(p1.Value, p2.Value);
                    decimal max = Math.Max(p1.Value, p2.Value);
                    decimal avg = Math.Round((min + max) / 2.0m, 2);

                    if ((hasBgn || convertBgnToEur) && !hasEur)
                    {
                        min = Math.Round(min / 1.95583m, 2);
                        max = Math.Round(max / 1.95583m, 2);
                        avg = Math.Round(avg / 1.95583m, 2);
                    }

                    string rangeText = $"€ {min:N0} – {max:N0}";
                    return (avg, min, max, rangeText);
                }
                if (p1.HasValue) return (p1.Value, p1.Value, p1.Value, null);
                if (p2.HasValue) return (p2.Value, p2.Value, p2.Value, null);
            }
        }

        var single = ParseSingleNumber(clean);
        if (single.HasValue)
        {
            decimal val = single.Value;
            if ((hasBgn || convertBgnToEur) && !hasEur)
            {
                val = Math.Round(val / 1.95583m, 2);
            }
            return (val, val, val, null);
        }

        return (null, null, null, null);
    }

    private static decimal? ParseNumberOrRange(string? input, bool convertBgnToEur = false)
    {
        return ParseNumberAndRange(input, convertBgnToEur).Avg;
    }

    private static decimal? ParseSingleNumber(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var s = Regex.Replace(input, @"[^\d.,\s]", "").Trim();
        if (string.IsNullOrWhiteSpace(s)) return null;

        s = Regex.Replace(s, @"\s+", "");

        if (s.Contains(',') && s.Contains('.'))
        {
            if (s.IndexOf(',') < s.IndexOf('.'))
            {
                s = s.Replace(",", "");
            }
            else
            {
                s = s.Replace(".", "").Replace(",", ".");
            }
        }
        else if (s.Contains(','))
        {
            var commaIdx = s.LastIndexOf(',');
            if (s.Count(c => c == ',') > 1 || (s.Length - commaIdx - 1 == 3 && commaIdx > 0))
            {
                s = s.Replace(",", "");
            }
            else
            {
                s = s.Replace(",", ".");
            }
        }
        else if (s.Contains('.'))
        {
            var dotIdx = s.LastIndexOf('.');
            if (s.Count(c => c == '.') > 1 || (s.Length - dotIdx - 1 == 3 && dotIdx > 0))
            {
                s = s.Replace(".", "");
            }
        }

        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
        {
            return val;
        }

        return null;
    }

    private static bool IsSummaryOrTotalRow(string skuCode, string title, string[] allCells)
    {
        var combined = (skuCode + " " + title).Trim().ToLowerInvariant();
        
        string[] totalPrefixes = { 
            "общо", "всичко", "тотал", "total", "subtotal", 
            "междинна сума", "крайна сума", "сума:",
            "в български лева", "в лева", "в евро", "с ддс", "без ддс", "ддс" 
        };

        if (totalPrefixes.Any(p => combined.StartsWith(p)))
        {
            return true;
        }

        foreach (var cell in allCells)
        {
            var cleaned = CleanMarkdown(cell).ToLowerInvariant().Trim(':', ' ');
            if (totalPrefixes.Any(p => cleaned == p || cleaned.StartsWith(p)))
            {
                return true;
            }
        }

        return false;
    }

    private static string CleanMarkdown(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var cleaned = input.Trim();
        cleaned = Regex.Replace(cleaned, @"\$(?:\\text\{\s*m\s*\}\^3|m\^3)\$", "m³");
        cleaned = Regex.Replace(cleaned, @"\$(?:\\text\{\s*m\s*\}\^2|m\^2)\$", "m²");
        cleaned = Regex.Replace(cleaned, @"\$([0-9]+)\s*\\text\{\s*m\s*\}\^3\$", "$1 m³");
        cleaned = Regex.Replace(cleaned, @"\$([0-9]+)\s*\\text\{\s*m\s*\}\^2\$", "$1 m²");
        cleaned = Regex.Replace(cleaned, @"\$([^$]+)\$", "$1");
        cleaned = Regex.Replace(cleaned, @"\*\*([^*]+)\*\*", "$1");
        cleaned = Regex.Replace(cleaned, @"\*([^*]+)\*", "$1");
        cleaned = Regex.Replace(cleaned, @"__([^_]+)__", "$1");
        cleaned = Regex.Replace(cleaned, @"_([^_]+)_", "$1");
        cleaned = Regex.Replace(cleaned, @"`([^`]+)`", "$1");
        cleaned = Regex.Replace(cleaned, @"\[([^\]]+)\]\([^)]+\)", "$1");
        return cleaned.Trim();
    }

    private static string[] SplitTableRow(string row)
    {
        var trimmed = row.Trim();
        if (trimmed.StartsWith("|")) trimmed = trimmed.Substring(1);
        if (trimmed.EndsWith("|")) trimmed = trimmed.Substring(0, trimmed.Length - 1);

        return trimmed.Split('|').Select(c => c.Trim()).ToArray();
    }

    public class RawParsedMarkdown
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
        public List<RawSection> RawSections { get; set; } = new();
    }

    public class RawSection
    {
        public string CategoryName { get; set; } = string.Empty;
        public List<RawItem> RawItems { get; set; } = new();
    }

    public class RawItem
    {
        public string SkuCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Unit { get; set; } = "бр.";
        public decimal Quantity { get; set; }
        public decimal? CustomUnitPriceEur { get; set; }
        public decimal? MinPriceEur { get; set; }
        public decimal? MaxPriceEur { get; set; }
        public string? PriceRangeText { get; set; }
        public List<string> SubItems { get; set; } = new();
        public bool IsPercentageOrBudgetGroup { get; set; }
    }
}
