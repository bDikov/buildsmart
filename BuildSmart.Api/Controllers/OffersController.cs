using Microsoft.AspNetCore.Mvc;
using BuildSmart.Core.Application.DTOs;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Application.Resources;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BuildSmart.Infrastructure.Services;

namespace BuildSmart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OffersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPdfGeneratorService _pdfGeneratorService;
    private readonly IStringLocalizer<OfferResources> _localizer;
    private readonly IAiService _aiService;

    public OffersController(
        IUnitOfWork unitOfWork,
        IPdfGeneratorService pdfGeneratorService,
        IStringLocalizer<OfferResources> localizer,
        IAiService aiService)
    {
        _unitOfWork = unitOfWork;
        _pdfGeneratorService = pdfGeneratorService;
        _localizer = localizer;
        _aiService = aiService;
    }

    [HttpGet("{projectId}/download")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadOfferPdf(Guid projectId, [FromQuery] bool force = false)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
        if (project == null)
        {
            return NotFound("Project not found.");
        }

        if (force)
        {
            project.MasterOfferPdf = null;
            var summaryOrDesc = project.GeneralSummary ?? project.Description;
            bool isExistingMarkdown = !string.IsNullOrWhiteSpace(summaryOrDesc) && 
                (summaryOrDesc.Contains("# ") || summaryOrDesc.Contains("## ") || summaryOrDesc.Contains("|"));
            if (!isExistingMarkdown)
            {
                project.GeneralSummary = null;
            }
        }

        if (project.MasterOfferPdf != null && project.MasterOfferPdf.Length > 0)
        {
            return File(project.MasterOfferPdf, "application/pdf", $"{project.Title}_Offer.pdf");
        }

        if (project.JobPosts != null && project.JobPosts.Any(jp => jp.Status == JobPostStatus.Draft))
        {
            return BadRequest("Cannot download offer until all categories are filled out.");
        }

        var activeJobPosts = (project.JobPosts?.ToList() ?? new List<JobPost>())
            .Where(jp => jp.Status != JobPostStatus.Cancelled && jp.CategoryStatus != ProjectCategoryStatus.Draft)
            .ToList();
        bool hasTasks = activeJobPosts.Any(j => j.JobTasks != null && j.JobTasks.Any());
        var projectCalcs = (await _unitOfWork.AiCalculations.GetByProjectWithTasksAsync(projectId)).ToList();
        bool hasValidCalcs = projectCalcs.Any(c => c.Tasks != null && c.Tasks.Any());

        if (!hasTasks && !hasValidCalcs)
        {
            return BadRequest("No categories have been priced yet for this project. Please run calculations first.");
        }

        // Check if project has structured markdown stored in GeneralSummary or Description
        var rawMarkdown = project.GeneralSummary ?? project.Description;
        MarkdownOfferParserService.RawParsedMarkdown? structuredData = null;
        if (!string.IsNullOrWhiteSpace(rawMarkdown) && (rawMarkdown.Contains("# ") || rawMarkdown.Contains("## ") || rawMarkdown.Contains("|")))
        {
            structuredData = MarkdownOfferParserService.ParseMarkdownRaw(rawMarkdown);
        }

        string currencySymbol = "€";
        decimal grandTotal = 0;
        var categoriesData = new List<object>();

        var targetLang = project.LanguageCode ?? "bg";
        bool isBg = targetLang.StartsWith("bg", StringComparison.OrdinalIgnoreCase);

        Func<string, string> localizeCriteria = (desc) =>
        {
            if (string.IsNullOrWhiteSpace(desc)) return desc;

            if (isBg && desc.StartsWith("Quality verification for ", StringComparison.OrdinalIgnoreCase))
            {
                string titlePart = desc;
                int startIndex = "Quality verification for ".Length;
                int endIndex = desc.IndexOf(" according to", StringComparison.OrdinalIgnoreCase);
                if (endIndex > startIndex)
                {
                    titlePart = desc.Substring(startIndex, endIndex - startIndex).Trim(' ', '"');
                }
                return $"Качествена проверка за \"{titlePart}\" съгласно Български Държавен Стандарт (БДС).";
            }
            else if (!isBg && desc.StartsWith("Качествена проверка за ", StringComparison.OrdinalIgnoreCase))
            {
                string titlePart = desc;
                int startIndex = "Качествена проверка за \"".Length;
                int endIndex = desc.IndexOf("\" съгласно", StringComparison.OrdinalIgnoreCase);
                if (endIndex > startIndex)
                {
                    titlePart = desc.Substring(startIndex, endIndex - startIndex).Trim(' ', '"');
                }
                return $"Quality verification for \"{titlePart}\" according to Bulgarian Construction Standards (BDS).";
            }
            return desc;
        };

        var categoryGroups = new Dictionary<string, (decimal Subtotal, List<object> Tasks)>();

        decimal adminMarkupPercentage = project.AdminMarkupPercentage;
        decimal markupFactor = 1.0m + (adminMarkupPercentage / 100.0m);
        decimal tasksMinTotal = 0m;
        decimal tasksMaxTotal = 0m;
        bool anyTaskHasRange = false;

        if (hasValidCalcs)
        {
            foreach (var calc in projectCalcs)
            {
                if (calc.Tasks == null || !calc.Tasks.Any()) continue;

                var category = await _unitOfWork.ServiceCategories.GetByIdAsync(calc.ServiceCategoryId);
                var matchingJp = activeJobPosts.FirstOrDefault(j => j.ServiceCategoryId == calc.ServiceCategoryId);
                if (matchingJp != null && matchingJp.CategoryStatus == ProjectCategoryStatus.Draft) continue;

                var categoryName = category?.Name ?? "General";
                if (category != null && !isBg && !string.IsNullOrEmpty(category.EnglishName))
                {
                    categoryName = category.EnglishName;
                }
                if (matchingJp != null && matchingJp.CategoryStatus == ProjectCategoryStatus.Pending)
                {
                    categoryName += isBg ? " (Чака активиране)" : " (Pending Activation)";
                }

                decimal categorySubtotal = 0m;
                var tasksForCategory = new List<object>();
                int taskSeqIndex = 1;

                foreach (var task in calc.Tasks.OrderBy(t => t.SequenceOrder))
                {
                    decimal effectivePrice = task.EstimatedPrice;
                    categorySubtotal += effectivePrice;

                    string? priceRange = null;
                    string? amountRange = null;
                    if (!string.IsNullOrWhiteSpace(task.Description))
                    {
                        var rangeMatch = Regex.Match(task.Description, @"\[RANGE:\s*([^\]]+)\]");
                        if (rangeMatch.Success)
                        {
                            priceRange = rangeMatch.Groups[1].Value.Trim();
                            amountRange = priceRange;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(priceRange))
                    {
                        var range = MarkdownOfferParserService.ParseNumberAndRange(priceRange);
                        if (range.Min.HasValue && range.Max.HasValue)
                        {
                            tasksMinTotal += range.Min.Value;
                            tasksMaxTotal += range.Max.Value;
                            if (range.Max.Value > range.Min.Value)
                            {
                                anyTaskHasRange = true;
                            }
                        }
                        else
                        {
                            tasksMinTotal += effectivePrice;
                            tasksMaxTotal += effectivePrice;
                        }
                    }
                    else
                    {
                        tasksMinTotal += effectivePrice;
                        tasksMaxTotal += effectivePrice;
                    }

                    tasksForCategory.Add(new
                    {
                        Index = taskSeqIndex++,
                        SkuCode = string.Empty,
                        Description = task.Title,
                        Unit = "бр.",
                        Quantity = "1.00",
                        UnitPrice = effectivePrice.ToString("N2", CultureInfo.InvariantCulture),
                        Amount = effectivePrice.ToString("N2", CultureInfo.InvariantCulture),
                        PriceRange = priceRange,
                        AmountRange = amountRange,
                        AcceptanceCriteria = task.AcceptanceCriteria?.Select(c => localizeCriteria(c.Description)).ToList() ?? new List<string>()
                    });
                }

                if (matchingJp == null || matchingJp.CategoryStatus == ProjectCategoryStatus.Active)
                {
                    grandTotal += categorySubtotal;
                }

                if (!categoryGroups.TryGetValue(categoryName, out var group))
                {
                    group = (0m, new List<object>());
                }
                group.Subtotal += categorySubtotal;
                group.Tasks.AddRange(tasksForCategory);
                categoryGroups[categoryName] = group;
            }
        }
        else
        {
            // Fallback directly to JobPosts and firmed JobTasks (e.g. for predefined projects)
            foreach (var jp in activeJobPosts)
            {
                var tasks = (jp.JobTasks?.OrderBy(t => t.SequenceOrder) ?? Enumerable.Empty<JobTask>()).ToList();
                if (!tasks.Any()) continue;

                string categoryName = !string.IsNullOrWhiteSpace(jp.Title)
                    ? jp.Title.Replace(" - Work Package", "").Trim()
                    : (jp.ServiceCategory?.Name ?? "General");

                if (categoryName.StartsWith("Cat ", StringComparison.OrdinalIgnoreCase) || categoryName.Equals("General", StringComparison.OrdinalIgnoreCase))
                {
                    categoryName = isBg ? "Количествено-Стойностна Сметка (КСС)" : "Bill of Quantities (BOQ)";
                }

                if (jp.ServiceCategory != null && !isBg && !string.IsNullOrEmpty(jp.ServiceCategory.EnglishName) && categoryName == jp.ServiceCategory.Name)
                {
                    categoryName = jp.ServiceCategory.EnglishName;
                }
                if (jp.CategoryStatus == ProjectCategoryStatus.Pending)
                {
                    categoryName += isBg ? " (Чака активиране)" : " (Pending Activation)";
                }

                decimal subtotal = 0m;
                var tasksForCategory = new List<object>();
                int taskSeqIndex = 1;

                foreach (var task in tasks)
                {
                    decimal effectivePrice;
                    if (task.TradesmanPrice > 0)
                    {
                        effectivePrice = (task.EstimatedPrice > task.TradesmanPrice)
                            ? task.EstimatedPrice
                            : Math.Round(task.TradesmanPrice * markupFactor, 2);
                    }
                    else
                    {
                        effectivePrice = task.EstimatedPrice > 0
                            ? task.EstimatedPrice
                            : 0m;
                    }

                    subtotal += effectivePrice;

                    var skuItem = task.SkuItems?.FirstOrDefault();
                    decimal qty = skuItem?.Quantity ?? 1m;
                    string unit = skuItem?.ServiceSku?.UnitType ?? "к-т";

                    string skuCode = skuItem?.ServiceSku?.SkuCode ?? string.Empty;
                    if (skuCode.StartsWith("CUSTOM-", StringComparison.OrdinalIgnoreCase) || 
                        skuCode.StartsWith("KSS-", StringComparison.OrdinalIgnoreCase) ||
                        skuCode.StartsWith("PANT-PAINT-WHITE", StringComparison.OrdinalIgnoreCase))
                    {
                        skuCode = string.Empty;
                    }

                    string? priceRange = null;
                    string? amountRange = null;
                    if (!string.IsNullOrWhiteSpace(task.Description))
                    {
                        var unitMatch = Regex.Match(task.Description, @"\[UNIT:\s*([^\]]+)\]");
                        if (unitMatch.Success) unit = unitMatch.Groups[1].Value.Trim();

                        var qtyMatch = Regex.Match(task.Description, @"\[QTY:\s*([\d\.,]+)\]");
                        if (qtyMatch.Success && decimal.TryParse(qtyMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedQty) && parsedQty > 0)
                        {
                            qty = parsedQty;
                        }

                        var rangeMatch = Regex.Match(task.Description, @"\[RANGE:\s*([^\]]+)\]");
                        if (rangeMatch.Success)
                        {
                            priceRange = rangeMatch.Groups[1].Value.Trim();
                            amountRange = priceRange;
                        }
                    }

                    if (unit == "sqm" || unit == "m2" || unit == "кв.м" || unit == "кв.м.") unit = "м²";
                    else if (unit == "m" || unit == "meter" || unit == "лм" || unit == "л.м.") unit = "м";
                    else if (unit == "pcs") unit = "бр.";
                    else if (unit == "set") unit = "к-т";

                    decimal unitPrice = qty > 0 ? Math.Round(effectivePrice / qty, 2) : effectivePrice;

                    if (!string.IsNullOrWhiteSpace(priceRange))
                    {
                        var range = MarkdownOfferParserService.ParseNumberAndRange(priceRange);
                        if (range.Min.HasValue && range.Max.HasValue)
                        {
                            tasksMinTotal += range.Min.Value * qty;
                            tasksMaxTotal += range.Max.Value * qty;
                            if (range.Max.Value > range.Min.Value)
                            {
                                anyTaskHasRange = true;
                            }
                        }
                        else
                        {
                            tasksMinTotal += effectivePrice;
                            tasksMaxTotal += effectivePrice;
                        }
                    }
                    else
                    {
                        tasksMinTotal += effectivePrice;
                        tasksMaxTotal += effectivePrice;
                    }

                    tasksForCategory.Add(new
                    {
                        Index = taskSeqIndex++,
                        SkuCode = skuCode,
                        Description = task.Title,
                        Unit = unit,
                        Quantity = qty.ToString("0.00", CultureInfo.InvariantCulture),
                        UnitPrice = unitPrice.ToString("N2", CultureInfo.InvariantCulture),
                        Amount = effectivePrice.ToString("N2", CultureInfo.InvariantCulture),
                        PriceRange = priceRange,
                        AmountRange = amountRange,
                        AcceptanceCriteria = task.AcceptanceCriteria?.Select(c => localizeCriteria(c.Description)).ToList() ?? new List<string>()
                    });
                }

                if (jp.CategoryStatus == ProjectCategoryStatus.Active)
                {
                    grandTotal += subtotal;
                }

                if (!categoryGroups.TryGetValue(categoryName, out var group))
                {
                    group = (0m, new List<object>());
                }
                group.Subtotal += subtotal;
                group.Tasks.AddRange(tasksForCategory);
                categoryGroups[categoryName] = group;
            }
        }

        foreach (var kvp in categoryGroups)
        {
            categoriesData.Add(new
            {
                CategoryName = kvp.Key,
                Subtotal = kvp.Value.Subtotal.ToString("N2"),
                SubtotalRange = (kvp.Value.Subtotal == 0m && structuredData?.GrandTotalRangeText != null) ? structuredData.GrandTotalRangeText : null,
                SubtotalLabel = string.Format(_localizer["Label_Subtotal"].Value, kvp.Key),
                Tasks = kvp.Value.Tasks
            });
        }

        var homeowner = await _unitOfWork.Users.GetByIdAsync(project.HomeownerId);
        string defaultClientTitle = isBg ? "Възложител" : "Client";
        string? clientName = null;

        if (!string.IsNullOrWhiteSpace(structuredData?.ClientName) &&
            !structuredData.ClientName.Equals("Уважаеми Клиент", StringComparison.OrdinalIgnoreCase) &&
            !structuredData.ClientName.Equals("Valued Client", StringComparison.OrdinalIgnoreCase) &&
            !structuredData.ClientName.Equals("Възложител", StringComparison.OrdinalIgnoreCase))
        {
            clientName = structuredData.ClientName.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(rawMarkdown))
        {
            var clientMatch = Regex.Match(rawMarkdown, @"(?im)^[-*]?\s*(?:\*\*)?(?:Клиент|Възложител|Собственик|Инвеститор|Получател|Заявител|Поръчител|Изготвено\s+за|Подготвено\s+за|Client|Owner|Prepared\s+for)(?:\*\*)?[:\s]+\*?\*?([^\r\n*]+)");
            if (clientMatch.Success && !string.IsNullOrWhiteSpace(clientMatch.Groups[1].Value))
            {
                var candidate = clientMatch.Groups[1].Value.Trim();
                if (!candidate.Equals("Уважаеми Клиент", StringComparison.OrdinalIgnoreCase) &&
                    !candidate.Equals("Valued Client", StringComparison.OrdinalIgnoreCase) &&
                    !candidate.Equals("Възложител", StringComparison.OrdinalIgnoreCase))
                {
                    clientName = candidate;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(clientName) && homeowner != null && homeowner.Role != UserRoleTypes.Admin && !string.IsNullOrWhiteSpace(homeowner.FirstName))
        {
            clientName = $"{homeowner.FirstName} {homeowner.LastName}".Trim();
        }

        if (string.IsNullOrWhiteSpace(clientName))
        {
            clientName = defaultClientTitle;
        }

        var clientAddress = !string.IsNullOrWhiteSpace(structuredData?.Parameters?.Location)
            ? structuredData.Parameters.Location
            : (!string.IsNullOrWhiteSpace(structuredData?.SiteAddress)
                ? structuredData.SiteAddress
                : (project.JobPosts?.FirstOrDefault()?.Location ?? homeowner?.Location ?? "гр. София"));

        var combinedScope = new StringBuilder();
        foreach (var jp in activeJobPosts.Where(j => !string.IsNullOrWhiteSpace(j.GeneratedScope)))
        {
            combinedScope.AppendLine($"## {jp.Title}");
            combinedScope.AppendLine(jp.GeneratedScope);
            combinedScope.AppendLine();
        }

        if (combinedScope.Length == 0)
        {
            foreach (var jp in activeJobPosts)
            {
                combinedScope.AppendLine($"## {jp.Title}");
                foreach (var task in jp.JobTasks ?? Enumerable.Empty<JobTask>())
                {
                    combinedScope.AppendLine($"- {task.Title}: {task.Description}");
                }
                combinedScope.AppendLine();
            }
        }

        string finalScopeDescription = project.Description;
        bool isStructuredOffer = structuredData != null && (structuredData.InvestmentBreakdown.Any() || structuredData.EngineeringNotes.Any());
        if (!isStructuredOffer && combinedScope.Length > 0)
        {
            try
            {
                finalScopeDescription = await _aiService.GenerateExecutiveSummaryAsync(combinedScope.ToString(), project.LanguageCode ?? "bg");
            }
            catch (Exception)
            {
                if (string.IsNullOrWhiteSpace(finalScopeDescription))
                {
                    finalScopeDescription = combinedScope.ToString();
                }
            }
        }

        // Page break decisions
        bool showPageBreakAfterBrief = true;
        bool showPageBreakAfterPricing = false;

        string briefClass = "pdf-page-flow";
        string pricingClass = "pdf-page-flow";
        string termsClass = "pdf-page-fixed";

        var grandTotalRange = structuredData?.GrandTotalRangeText;
        var grandTotalBgnRange = structuredData?.GrandTotalBgnRangeText;
        var averagePricePerSqm = structuredData?.AveragePricePerSqm;
        var parameters = structuredData?.Parameters;
        var investmentBreakdown = structuredData?.InvestmentBreakdown;
        var engineeringNotes = structuredData?.EngineeringNotes;

        string FormatInt(decimal val) => isBg 
            ? val.ToString("#,##0", new NumberFormatInfo { NumberGroupSeparator = " " }) 
            : val.ToString("N0", CultureInfo.InvariantCulture);

        if (anyTaskHasRange && tasksMaxTotal > tasksMinTotal)
        {
            grandTotalRange = $"€ {FormatInt(tasksMinTotal)} – € {FormatInt(tasksMaxTotal)}";
            grandTotalBgnRange = $"{FormatInt(Math.Round(tasksMinTotal * 1.95583m, 0))} – {FormatInt(Math.Round(tasksMaxTotal * 1.95583m, 0))} лв.";

            if (investmentBreakdown != null && investmentBreakdown.Any())
            {
                foreach (var comp in investmentBreakdown)
                {
                    var match = Regex.Match(comp.SharePercentage, @"(\d+)");
                    if (match.Success && decimal.TryParse(match.Groups[1].Value, out var pct))
                    {
                        decimal share = pct / 100m;
                        decimal cMin = Math.Round(tasksMinTotal * share, 0);
                        decimal cMax = Math.Round(tasksMaxTotal * share, 0);
                        comp.AmountEurRange = $"€ {FormatInt(cMin)} – € {FormatInt(cMax)}";
                        comp.AmountBgnRange = $"{FormatInt(Math.Round(cMin * 1.95583m, 0))} – {FormatInt(Math.Round(cMax * 1.95583m, 0))} лв.";
                    }
                }
            }

            if (parameters != null && !string.IsNullOrWhiteSpace(parameters.TotalArea))
            {
                var areaMatch = Regex.Match(parameters.TotalArea, @"([\d\.,]+)");
                if (areaMatch.Success && decimal.TryParse(areaMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var area) && area > 0)
                {
                    decimal minPerSqm = Math.Round(tasksMinTotal / area, 0);
                    decimal maxPerSqm = Math.Round(tasksMaxTotal / area, 0);
                    decimal minBgnPerSqm = Math.Round(minPerSqm * 1.95583m, 0);
                    decimal maxBgnPerSqm = Math.Round(maxPerSqm * 1.95583m, 0);
                    averagePricePerSqm = $"Средна цена: {FormatInt(minPerSqm)} – {FormatInt(maxPerSqm)} €/m² ({FormatInt(minBgnPerSqm)} – {FormatInt(maxBgnPerSqm)} лв./m²)";
                }
            }

            if (investmentBreakdown != null && !string.IsNullOrWhiteSpace(averagePricePerSqm))
            {
                var totalComp = investmentBreakdown.FirstOrDefault(c => c.Name.Contains("ОБЩО", StringComparison.OrdinalIgnoreCase) || c.SharePercentage.Contains("100"));
                if (totalComp != null)
                {
                    totalComp.Description = averagePricePerSqm;
                }
            }
        }

        if (engineeringNotes != null && engineeringNotes.Any() && !string.IsNullOrWhiteSpace(grandTotalRange))
        {
            foreach (var note in engineeringNotes)
            {
                if (!string.IsNullOrWhiteSpace(note.Title) &&
                    (note.Title.Contains("Защо офертата е в диапазона", StringComparison.OrdinalIgnoreCase) ||
                     note.Title.Contains("Why the offer is in the range", StringComparison.OrdinalIgnoreCase)))
                {
                    note.Title = Regex.Replace(note.Title, @"€\s*[0-9\s]+[–-]\s*€?\s*[0-9\s]+", grandTotalRange);
                }
            }
        }

        string GetLoc(string key, string bgDefault, string enDefault)
        {
            var val = _localizer[key]?.Value;
            if (string.IsNullOrWhiteSpace(val) || val == key)
                return isBg ? bgDefault : enDefault;
            return val;
        }

        var offerData = new
        {
            Header_Hello = GetLoc("Header_Hello", "здравейте.", "hello."),
            Header_PreparedBy = GetLoc("Header_PreparedBy", "Изготвено от", "Prepared by"),
            Header_ProjectProposal = isStructuredOffer 
                ? (isBg ? "ОФИЦИАЛНА ОФЕРТА И КОЛИЧЕСТВЕНО-СТОЙНОСТНА СМЕТКА (КСС)" : "OFFICIAL OFFER AND BILL OF QUANTITIES (BOQ)")
                : GetLoc("Header_ProjectProposal", "ПРОЕКТНО ПРЕДЛОЖЕНИЕ ЗА ИЗПЪЛНЕНИЕ НА СМР", "PROJECT PROPOSAL"),
            Header_Overview = GetLoc("Header_Overview", "Преглед", "Overview"),
            Header_PreparedFor = GetLoc("Header_PreparedFor", "Изготвено за", "Prepared for"),
            Header_Fees = GetLoc("Header_Fees", "Услуги", "Services"),
            Label_FeesDescription = GetLoc("Label_FeesDescription", "Ето разбивка на задачите по категории.", "Here is a breakdown of tasks by category."),
            Label_GrandTotal = GetLoc("Label_GrandTotal", "Обща сума", "Grand Total"),
            Header_Terms = GetLoc("Header_Terms", "Правила и Условия", "Terms & Conditions"),

            Terms_Intro = GetLoc("Terms_Intro", "Настоящият документ представлява необвързваща индикативна оферта, която служи като професионален план за Вашия проект. Тези стойности са прогнозни и поставят експертната основа за реализацията на Вашата визия.", "This document represents a non-binding indicative offer that serves as a professional roadmap for your project. These values are estimates and establish the expert foundation for bringing your vision to life."),
            Terms_Point1 = GetLoc("Terms_Point1", "Без авансови плащания (0% аванс): Вие плащате единствено за напълно завършени и приети от Вас етапи. Вашите средства са защитени в нашата сигурна система.", "Zero upfront payments (0% advance): You only pay for fully completed and approved stages. Your funds are protected in our secure escrow system."),
            Terms_Point2 = GetLoc("Terms_Point2", "Фиксирана цена след оглед: Цената, определена след огледа на място, е крайна и няма да се промени в процеса на работа, без Вашето изрично писмено съгласие.", "Fixed price guarantee: The final price established after the on-site inspection is fixed and will not change during execution without your written consent."),
            Terms_Point3 = GetLoc("Terms_Point3", "14 дни гарантирана оферта: Изчислените цени и условия са валидни и защитени за период от 14 дни от датата на издаване на тази оферта.", "14-day price lock: The estimated prices and terms are guaranteed for 14 days from the date of issuance."),
            Terms_Point4 = GetLoc("Terms_Point4", "Професионален технически контрол и гаранция: Всеки етап от работата се проверява от наш независим технически ръководител преди приемане и изплащане.", "Independent technical supervision: Every stage is inspected by an independent certified engineer before approval and payout."),
            Terms_Point5 = GetLoc("Terms_Point5", "Прозрачност на материалите: Цените представляват само труд, което Ви дава свобода сами да изберете финалните материали или да използвате нашите партньорски отстъпки.", "Material transparency: All labor costs are separated from materials, giving you complete freedom to purchase finishes directly or use our trade discounts."),

            Footer_Validity = GetLoc("Footer_Validity", "Този документ е необвързваща индикативна оферта и не представлява договор за изпълнение. Крайните цени подлежат на уточнение след оглед и приключване на тържна процедура.", "This document is a non-binding estimate and does not constitute an execution contract. Final pricing is confirmed after on-site inspection."),
            Label_ProjectBrief = isStructuredOffer ? (isBg ? "01 — ОБЩИ ДАННИ И ПАРАМЕТРИ" : "01 — PROJECT PARAMETERS") : GetLoc("Label_ProjectBrief", "01 — ПРОЕКТНА ЗАДАЧА", "01 — PROJECT BRIEF"),
            Label_PricingBreakdown = isStructuredOffer 
                ? (isBg ? "03 — КОЛИЧЕСТВЕНО-СТОЙНОСТНА СМЕТКА (КСС)" : "03 — BILL OF QUANTITIES (BOQ)") 
                : GetLoc("Label_PricingBreakdown", "02 — ЦЕНИ И РАЗБИВКА", "02 — PRICING & BREAKDOWN"),
            Label_TC = (isStructuredOffer && engineeringNotes?.Any() == true) ? (isBg ? "05 — ОБЩИ УСЛОВИЯ" : "05 — TERMS & CONDITIONS") : (isStructuredOffer ? (isBg ? "04 — ОБЩИ УСЛОВИЯ" : "04 — TERMS & CONDITIONS") : GetLoc("Label_TC", "03 — ОБЩИ УСЛОВИЯ", "03 — TERMS & CONDITIONS")),

            CurrencySymbol = currencySymbol,

            JobTitle = project.Title,
            JobId = project.Id.ToString().Substring(0, 8),
            TradesmanName = GetLoc("Label_SystemEstimate", "Системна Оценка на BuildSmart", "BuildSmart System Estimate"),
            Date = System.Globalization.CultureInfo.CurrentCulture.Name.StartsWith("bg", StringComparison.OrdinalIgnoreCase)
                ? DateTime.UtcNow.ToString("dd.MM.yyyy")
                : DateTime.UtcNow.ToString("MMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture),
            ClientName = clientName,
            ClientAddress = clientAddress,
            ScopeDescription = finalScopeDescription,
            Categories = categoriesData,
            SubtotalAmount = grandTotal.ToString("N2"),
            TotalAmount = grandTotal.ToString("N2"),
            GrandTotalRange = grandTotalRange,
            GrandTotalBgnRange = grandTotalBgnRange,
            AveragePricePerSqm = averagePricePerSqm,

            HasParameters = parameters != null && (!string.IsNullOrEmpty(parameters.Location) || !string.IsNullOrEmpty(parameters.TotalArea)),
            Parameters = parameters,

            HasInvestmentBreakdown = investmentBreakdown != null && investmentBreakdown.Any(),
            InvestmentBreakdown = investmentBreakdown,

            HasEngineeringNotes = engineeringNotes != null && engineeringNotes.Any(),
            EngineeringNotes = engineeringNotes,

            ShowPageBreakAfterBrief = showPageBreakAfterBrief,
            ShowPageBreakAfterPricing = showPageBreakAfterPricing,
            BriefClass = briefClass,
            PricingClass = pricingClass,
            TermsClass = termsClass
        };

        byte[] pdfBytes = await _pdfGeneratorService.GenerateOfferPdfAsync(offerData);

        var activeCategoryIds = activeJobPosts.Select(j => j.ServiceCategoryId).Distinct().ToList();
        var pricedCategoryIds = projectCalcs.Where(c => c.Tasks != null && c.Tasks.Any()).Select(c => c.ServiceCategoryId).Distinct().ToList();
        bool allActiveCategoriesArePriced = hasValidCalcs
            ? (activeCategoryIds.Any() && activeCategoryIds.All(id => pricedCategoryIds.Contains(id)))
            : (activeJobPosts.Any() && activeJobPosts.All(jp => jp.JobTasks != null && jp.JobTasks.Any()));

        if (allActiveCategoriesArePriced)
        {
            project.MasterOfferPdf = pdfBytes;
            if (string.IsNullOrWhiteSpace(project.GeneralSummary))
            {
                project.GeneralSummary = finalScopeDescription;
            }
            project.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Projects.Update(project);
            await _unitOfWork.SaveChangesAsync();
        }

        return File(pdfBytes, "application/pdf", $"{project.Title}_Offer.pdf");
    }

    [HttpPost("preview-markdown")]
    [Authorize(Roles = "Admin, ADMIN, admin")]
    public async Task<IActionResult> PreviewMarkdownOffer(
        [FromBody] MarkdownOfferRequest request,
        [FromServices] IMarkdownOfferParserService parserService)
    {
        if (string.IsNullOrWhiteSpace(request?.MarkdownContent))
        {
            return BadRequest("Markdown content cannot be empty.");
        }

        try
        {
            var preview = await parserService.PreviewMarkdownOfferAsync(request.MarkdownContent);
            if (!string.IsNullOrWhiteSpace(request.ClientName) &&
                !request.ClientName.Equals("Уважаеми Клиент", StringComparison.OrdinalIgnoreCase) &&
                !request.ClientName.Equals("Valued Client", StringComparison.OrdinalIgnoreCase) &&
                !request.ClientName.Equals("Възложител", StringComparison.OrdinalIgnoreCase))
            {
                preview.ClientName = request.ClientName.Trim();
            }
            if (!string.IsNullOrWhiteSpace(request.SiteAddress))
            {
                preview.SiteAddress = request.SiteAddress.Trim();
            }
            if (!string.IsNullOrWhiteSpace(request.ProjectTitle))
            {
                preview.ProjectTitle = request.ProjectTitle.Trim();
            }
            if (!string.IsNullOrWhiteSpace(request.AssignTo))
            {
                preview.AssignTo = request.AssignTo.Trim();
            }

            // Sync / fallback between ClientName and AssignTo
            if ((string.IsNullOrWhiteSpace(preview.ClientName) || preview.ClientName.Equals("Уважаеми Клиент", StringComparison.OrdinalIgnoreCase) || preview.ClientName.Equals("Възложител", StringComparison.OrdinalIgnoreCase))
                && !string.IsNullOrWhiteSpace(preview.AssignTo) && !preview.AssignTo.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                preview.ClientName = preview.AssignTo;
            }
            else if ((string.IsNullOrWhiteSpace(preview.AssignTo) || preview.AssignTo.Equals("admin", StringComparison.OrdinalIgnoreCase))
                && !string.IsNullOrWhiteSpace(preview.ClientName) && !preview.ClientName.Equals("Уважаеми Клиент", StringComparison.OrdinalIgnoreCase) && !preview.ClientName.Equals("Възложител", StringComparison.OrdinalIgnoreCase))
            {
                preview.AssignTo = preview.ClientName;
            }

            return Ok(preview);
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                msg += $" Details: {inner.Message}";
                inner = inner.InnerException;
            }
            return BadRequest(new { Error = msg });
        }
    }

    [HttpPost("import-markdown")]
    [Authorize(Roles = "Admin, ADMIN, admin")]
    public async Task<IActionResult> ImportMarkdownOffer(
        [FromBody] MarkdownOfferRequest request,
        [FromServices] IMarkdownOfferParserService parserService,
        [FromServices] IProjectManagementService projectManagementService,
        [FromServices] AppDbContext dbContext)
    {
        if (string.IsNullOrWhiteSpace(request?.MarkdownContent))
        {
            return BadRequest("Markdown content cannot be empty.");
        }

        try
        {
            var parsed = await parserService.ParseMarkdownOfferAsync(request.MarkdownContent);
            if (!parsed.Phases.Any() || !parsed.Phases.Any(p => p.Items.Any()))
            {
                return BadRequest("No valid work packages or line items found in the Markdown document.");
            }

            if (!string.IsNullOrWhiteSpace(request.ClientName) &&
                !request.ClientName.Equals("Уважаеми Клиент", StringComparison.OrdinalIgnoreCase) &&
                !request.ClientName.Equals("Valued Client", StringComparison.OrdinalIgnoreCase) &&
                !request.ClientName.Equals("Възложител", StringComparison.OrdinalIgnoreCase))
            {
                parsed.ClientName = request.ClientName.Trim();
            }
            if (!string.IsNullOrWhiteSpace(request.SiteAddress))
            {
                parsed.SiteAddress = request.SiteAddress.Trim();
            }
            if (!string.IsNullOrWhiteSpace(request.ProjectTitle))
            {
                parsed.ProjectTitle = request.ProjectTitle.Trim();
            }
            if (!string.IsNullOrWhiteSpace(request.AssignTo))
            {
                parsed.AssignTo = request.AssignTo.Trim();
            }

            // Sync / fallback between parsed.ClientName and parsed.AssignTo
            if ((string.IsNullOrWhiteSpace(parsed.ClientName) || 
                 parsed.ClientName.Equals("Уважаеми Клиент", StringComparison.OrdinalIgnoreCase) || 
                 parsed.ClientName.Equals("Възложител", StringComparison.OrdinalIgnoreCase) ||
                 parsed.ClientName.Equals("Valued Client", StringComparison.OrdinalIgnoreCase))
                && !string.IsNullOrWhiteSpace(parsed.AssignTo) && !parsed.AssignTo.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                parsed.ClientName = parsed.AssignTo.Trim();
            }
            else if ((string.IsNullOrWhiteSpace(parsed.AssignTo) || parsed.AssignTo.Equals("admin", StringComparison.OrdinalIgnoreCase))
                && !string.IsNullOrWhiteSpace(parsed.ClientName) && 
                !parsed.ClientName.Equals("Уважаеми Клиент", StringComparison.OrdinalIgnoreCase) && 
                !parsed.ClientName.Equals("Възложител", StringComparison.OrdinalIgnoreCase) &&
                !parsed.ClientName.Equals("Valued Client", StringComparison.OrdinalIgnoreCase))
            {
                parsed.AssignTo = parsed.ClientName.Trim();
            }

            // Apply line item overrides if provided from Admin UI preview
            if (request.ItemOverrides != null && request.ItemOverrides.Any())
            {
                var allItems = parsed.Phases.SelectMany(p => p.Items).ToList();
                foreach (var ovr in request.ItemOverrides)
                {
                    CustomOfferItemDto? targetItem = null;
                    if (ovr.Index > 0)
                    {
                        targetItem = allItems.FirstOrDefault(i => i.Index == ovr.Index);
                    }
                    if (targetItem == null && !string.IsNullOrWhiteSpace(ovr.SkuCode))
                    {
                        targetItem = allItems.FirstOrDefault(i => string.Equals(i.SkuCode, ovr.SkuCode, StringComparison.OrdinalIgnoreCase));
                    }
                    if (targetItem == null && !string.IsNullOrWhiteSpace(ovr.Title))
                    {
                        targetItem = allItems.FirstOrDefault(i => string.Equals(i.Title, ovr.Title, StringComparison.OrdinalIgnoreCase));
                    }

                    if (targetItem != null)
                    {
                        if (ovr.Quantity.HasValue && ovr.Quantity.Value > 0)
                        {
                            targetItem.Quantity = ovr.Quantity.Value;
                        }

                        if (!string.IsNullOrWhiteSpace(ovr.PriceRangeText))
                        {
                            targetItem.PriceRangeText = ovr.PriceRangeText;
                            targetItem.MinPriceEur = ovr.MinUnitPriceEur;
                            targetItem.MaxPriceEur = ovr.MaxUnitPriceEur;
                            if (ovr.UnitPriceEur.HasValue && ovr.UnitPriceEur.Value > 0)
                            {
                                targetItem.UnitPriceEur = ovr.UnitPriceEur.Value;
                            }
                            else if (ovr.MinUnitPriceEur.HasValue && ovr.MaxUnitPriceEur.HasValue)
                            {
                                targetItem.UnitPriceEur = Math.Round((ovr.MinUnitPriceEur.Value + ovr.MaxUnitPriceEur.Value) / 2m, 2);
                            }

                            if (targetItem.Description.Contains("[RANGE:"))
                            {
                                targetItem.Description = Regex.Replace(targetItem.Description, @"\[RANGE:\s*[^\]]+\]", $"[RANGE: {ovr.PriceRangeText}]");
                            }
                            else
                            {
                                targetItem.Description = $"[RANGE: {ovr.PriceRangeText}] " + targetItem.Description;
                            }
                        }
                        else if (ovr.UnitPriceEur.HasValue && ovr.UnitPriceEur.Value > 0)
                        {
                            targetItem.UnitPriceEur = ovr.UnitPriceEur.Value;
                            targetItem.PriceRangeText = null;
                            targetItem.MinPriceEur = null;
                            targetItem.MaxPriceEur = null;
                            if (targetItem.Description.Contains("[RANGE:"))
                            {
                                targetItem.Description = Regex.Replace(targetItem.Description, @"\[RANGE:\s*[^\]]+\]\s*", "");
                            }
                        }
                    }
                }
            }

            // Determine target homeowner user
            User? targetUser = null;
            string? assignTo = parsed.AssignTo?.Trim();

            if (!string.IsNullOrWhiteSpace(assignTo) && !assignTo.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                bool isGuid = Guid.TryParse(assignTo, out var targetUserId);

                // 1. Match by Email or Guid
                targetUser = await dbContext.Users
                    .Include(u => u.HomeownerProfile)
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == assignTo.ToLower() || (isGuid && u.Id == targetUserId));

                // 2. Match by Full Name exact (FirstName + " " + LastName)
                if (targetUser == null)
                {
                    targetUser = await dbContext.Users
                        .Include(u => u.HomeownerProfile)
                        .FirstOrDefaultAsync(u => (u.FirstName + " " + (u.LastName ?? "")).Trim().ToLower() == assignTo.ToLower());
                }

                // 3. Match by First Name or Last Name
                if (targetUser == null)
                {
                    targetUser = await dbContext.Users
                        .Include(u => u.HomeownerProfile)
                        .FirstOrDefaultAsync(u => u.FirstName.ToLower() == assignTo.ToLower() || (u.LastName != null && u.LastName.ToLower() == assignTo.ToLower()));
                }

                // 4. Match by Substring / Contains on names
                if (targetUser == null)
                {
                    targetUser = await dbContext.Users
                        .Include(u => u.HomeownerProfile)
                        .FirstOrDefaultAsync(u => (u.FirstName + " " + (u.LastName ?? "")).ToLower().Contains(assignTo.ToLower()) ||
                                                  (!string.IsNullOrWhiteSpace(u.FirstName) && assignTo.ToLower().Contains(u.FirstName.ToLower())));
                }

                // 5. If user still not found, dynamically create a Homeowner user account with this name
                if (targetUser == null)
                {
                    var nameParts = assignTo.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    string fName = nameParts.Length > 0 ? nameParts[0] : assignTo;
                    string lName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : string.Empty;

                    string cleanSlug = Regex.Replace(fName.ToLowerInvariant(), @"[^a-z0-9]", "");
                    if (string.IsNullOrEmpty(cleanSlug)) cleanSlug = "client";
                    string clientEmail = assignTo.Contains('@') ? assignTo : $"{cleanSlug}_{Guid.NewGuid().ToString("N")[..6]}@client.buildsmart.bg";

                    targetUser = new User
                    {
                        Id = Guid.NewGuid(),
                        FirstName = fName,
                        LastName = lName,
                        Email = clientEmail,
                        Role = UserRoleTypes.Homeowner,
                        PreferredLanguage = "bg",
                        PreferredTheme = "system",
                        IsEmailVerified = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await dbContext.Users.AddAsync(targetUser);

                    var profile = new HomeownerProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = targetUser.Id,
                        Address = parsed.SiteAddress,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await dbContext.HomeownerProfiles.AddAsync(profile);
                    targetUser.HomeownerProfile = profile;
                    await dbContext.SaveChangesAsync();
                }
            }

            if (targetUser == null)
            {
                targetUser = await dbContext.Users
                    .Include(u => u.HomeownerProfile)
                    .FirstOrDefaultAsync(u => u.Role == UserRoleTypes.Admin)
                    ?? await dbContext.Users.Include(u => u.HomeownerProfile).FirstOrDefaultAsync();

                if (targetUser == null)
                {
                    return BadRequest("No system user found to assign the project to.");
                }
            }

            if (targetUser.HomeownerProfile == null)
            {
                var existingProfile = await dbContext.HomeownerProfiles.FirstOrDefaultAsync(hp => hp.UserId == targetUser.Id);
                if (existingProfile != null)
                {
                    targetUser.HomeownerProfile = existingProfile;
                }
                else
                {
                    var profile = new HomeownerProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = targetUser.Id,
                        Address = parsed.SiteAddress,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await dbContext.HomeownerProfiles.AddAsync(profile);
                    targetUser.HomeownerProfile = profile;
                    await dbContext.SaveChangesAsync();
                }
            }

            // If parsed.ClientName is still empty or placeholder, set from targetUser
            if (targetUser.Role != UserRoleTypes.Admin && !string.IsNullOrWhiteSpace(targetUser.FirstName))
            {
                if (string.IsNullOrWhiteSpace(parsed.ClientName) || 
                    parsed.ClientName.Equals("Уважаеми Клиент", StringComparison.OrdinalIgnoreCase) || 
                    parsed.ClientName.Equals("Valued Client", StringComparison.OrdinalIgnoreCase) ||
                    parsed.ClientName.Equals("Възложител", StringComparison.OrdinalIgnoreCase))
                {
                    parsed.ClientName = $"{targetUser.FirstName} {targetUser.LastName}".Trim();
                }
            }

            var effectiveMarkdown = InjectOrUpdateMarkdownMetadata(request.MarkdownContent, parsed.ClientName, parsed.SiteAddress, parsed.ProjectTitle, request.ItemOverrides);

            // Create Project from parsed phases
            var projectId = await projectManagementService.CreateProjectFromOfferTemplateAsync(
                targetUser.Id,
                parsed.ProjectTitle,
                effectiveMarkdown,
                parsed.SiteAddress ?? "София",
                null,
                parsed.AdminMarkupPercentage,
                parsed.Phases);

            // Pre-compile the Master Offer PDF immediately
            await DownloadOfferPdf(projectId, force: true);

            var project = await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == projectId);

            return Ok(new
            {
                Success = true,
                ProjectId = projectId,
                ProjectTitle = project?.Title ?? parsed.ProjectTitle,
                AssignedUser = $"{targetUser.FirstName} {targetUser.LastName} ({targetUser.Email})",
                DownloadUrl = $"/api/offers/{projectId}/download"
            });
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                msg += $" Details: {inner.Message}";
                inner = inner.InnerException;
            }
            return BadRequest(new { Error = msg });
        }
    }

    private static string InjectOrUpdateMarkdownMetadata(string markdown, string? clientName, string? siteAddress, string? projectTitle, List<MarkdownOfferItemOverride>? itemOverrides = null)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return markdown;

        var lines = markdown.Replace("\r\n", "\n").Split('\n').ToList();

        if (!string.IsNullOrWhiteSpace(clientName) &&
            !clientName.Equals("Уважаеми Клиент", StringComparison.OrdinalIgnoreCase) &&
            !clientName.Equals("Valued Client", StringComparison.OrdinalIgnoreCase) &&
            !clientName.Equals("Възложител", StringComparison.OrdinalIgnoreCase))
        {
            // 1. Update YAML Frontmatter if present
            if (lines.Count > 0 && lines[0].Trim() == "---")
            {
                int closingIndex = lines.FindIndex(1, l => l.Trim() == "---");
                if (closingIndex > 0)
                {
                    int clientLineIndex = lines.FindIndex(1, closingIndex - 1, l => 
                        l.StartsWith("ClientName:", StringComparison.OrdinalIgnoreCase) || 
                        l.StartsWith("Client:", StringComparison.OrdinalIgnoreCase) || 
                        l.StartsWith("Клиент:", StringComparison.OrdinalIgnoreCase) || 
                        l.StartsWith("Собственик:", StringComparison.OrdinalIgnoreCase) || 
                        l.StartsWith("Възложител:", StringComparison.OrdinalIgnoreCase));
                    if (clientLineIndex > 0)
                    {
                        lines[clientLineIndex] = $"ClientName: \"{clientName}\"";
                    }
                    else
                    {
                        lines.Insert(closingIndex, $"ClientName: \"{clientName}\"");
                    }
                }
            }

            // 2. Update Section 1 if present
            int s1Index = lines.FindIndex(l => l.Contains("1. ОБЩИ ДАННИ") || l.Contains("ОБЩИ ДАННИ"));
            if (s1Index >= 0)
            {
                int nextSectionIndex = lines.FindIndex(s1Index + 1, l => l.Trim().StartsWith("## ") || l.Trim().StartsWith("### 2.") || l.Trim().StartsWith("---"));
                int searchCount = nextSectionIndex > s1Index ? (nextSectionIndex - s1Index - 1) : (lines.Count - s1Index - 1);
                int clientLineIndex = searchCount > 0
                    ? lines.FindIndex(s1Index + 1, searchCount, l => 
                        l.Contains("Клиент") || l.Contains("Изготвено") || l.Contains("Подготвено") || 
                        l.Contains("Възложител") || l.Contains("Собственик") || l.Contains("Инвеститор") || 
                        l.Contains("Получател") || l.Contains("Заявител") || l.Contains("Поръчител") || 
                        l.Contains("Client") || l.Contains("Owner") || l.Contains("Prepared"))
                    : -1;

                if (clientLineIndex >= 0)
                {
                    lines[clientLineIndex] = $"- **Изготвено за:** {clientName}";
                }
                else
                {
                    lines.Insert(s1Index + 1, $"- **Изготвено за:** {clientName}");
                }
            }
            else if (lines.Count == 0 || lines[0].Trim() != "---")
            {
                int existingIndex = lines.FindIndex(l => 
                    l.StartsWith("Клиент:") || l.StartsWith("Изготвено за:") || 
                    l.StartsWith("Възложител:") || l.StartsWith("Собственик:"));
                if (existingIndex >= 0)
                {
                    lines[existingIndex] = $"Изготвено за: {clientName}";
                }
                else
                {
                    lines.Insert(0, $"Изготвено за: {clientName}\n");
                }
            }
        }

        if (itemOverrides != null && itemOverrides.Any())
        {
            // Restrict item price overrides to Section 4 (ОБОБЩЕНА ТАБЛИЦА) or КСС tables.
            // Never touch Section 2 (СТРУКТУРА НА ИНВЕСТИЦИЯТА) or investment breakdown tables.
            int s4Index = lines.FindIndex(l => l.Contains("4. ОБОБЩЕНА ТАБЛИЦА") || l.Contains("ОБОБЩЕНА ТАБЛИЦА"));
            int startIndex = s4Index >= 0 ? s4Index : 0;

            for (int i = startIndex; i < lines.Count; i++)
            {
                var line = lines[i];
                if (!line.Trim().StartsWith("|")) continue;

                var rawCells = line.Split('|').ToList();
                var cleanCells = rawCells.Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToList();
                if (cleanCells.Count < 2) continue;

                // Skip header or separator rows, and explicitly skip Section 2 investment breakdown rows
                if (cleanCells.Any(c => c.Contains("---") || 
                                       c.Contains("Какво включва", StringComparison.OrdinalIgnoreCase) || 
                                       c.Contains("Дял (%)", StringComparison.OrdinalIgnoreCase) || 
                                       c.Contains("Компонент на инвестицията", StringComparison.OrdinalIgnoreCase) ||
                                       c.Contains("СМР Раздел", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                foreach (var ovr in itemOverrides)
                {
                    bool match = false;
                    if (ovr.Index > 0 && int.TryParse(Regex.Replace(cleanCells[0], @"\D", ""), out int idx) && idx == ovr.Index)
                    {
                        match = true;
                    }
                    else if (!string.IsNullOrWhiteSpace(ovr.Title) && cleanCells.Any(c => c.Contains(ovr.Title, StringComparison.OrdinalIgnoreCase)))
                    {
                        match = true;
                    }

                    if (match)
                    {
                        string newPrice = !string.IsNullOrWhiteSpace(ovr.PriceRangeText) ? ovr.PriceRangeText : $"€ {ovr.UnitPriceEur:N2}";
                        for (int c = rawCells.Count - 1; c >= 0; c--)
                        {
                            if (!string.IsNullOrWhiteSpace(rawCells[c]))
                            {
                                rawCells[c] = $" {newPrice} ";
                                break;
                            }
                        }
                        lines[i] = string.Join("|", rawCells);
                        break;
                    }
                }
            }
        }

        return string.Join("\n", lines);
    }

    public class MarkdownOfferRequest
    {
        public string MarkdownContent { get; set; } = string.Empty;
        public string? ClientName { get; set; }
        public string? SiteAddress { get; set; }
        public string? ProjectTitle { get; set; }
        public string? AssignTo { get; set; }
        public List<MarkdownOfferItemOverride>? ItemOverrides { get; set; }
    }
}

