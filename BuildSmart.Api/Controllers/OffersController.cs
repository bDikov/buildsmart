using Microsoft.AspNetCore.Mvc;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Application.Resources;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            project.GeneralSummary = null;
        }

        if (project.MasterOfferPdf != null && project.MasterOfferPdf.Length > 0)
        {
            return File(project.MasterOfferPdf, "application/pdf", $"{project.Title}_Offer.pdf");
        }

        var activeJobPosts = (project.JobPosts?.ToList() ?? new List<JobPost>()).Where(jp => jp.Status != JobPostStatus.Cancelled).ToList();
        bool hasTasks = activeJobPosts.Any(j => j.JobTasks != null && j.JobTasks.Any());
        var projectCalcs = (await _unitOfWork.AiCalculations.GetByProjectWithTasksAsync(projectId)).ToList();
        bool hasValidCalcs = projectCalcs.Any(c => c.Tasks != null && c.Tasks.Any());

        if (project.JobPosts != null && project.JobPosts.Any(jp => jp.Status == JobPostStatus.Draft))
        {
            return BadRequest("Cannot download offer until all categories are filled out.");
        }

        if (!hasTasks && !hasValidCalcs)
        {
            return BadRequest("No categories have been priced yet for this project. Please run calculations first.");
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

        decimal adminMarkupPercentage = project.AdminMarkupPercentage > 0 ? project.AdminMarkupPercentage : 20.0m;
        decimal markupFactor = 1.0m + (adminMarkupPercentage / 100.0m);

        if (hasValidCalcs)
        {
            foreach (var calc in projectCalcs)
            {
                if (calc.Tasks == null || !calc.Tasks.Any()) continue;

                var category = await _unitOfWork.ServiceCategories.GetByIdAsync(calc.ServiceCategoryId);
                var categoryName = category?.Name ?? "General";
                if (category != null && !isBg && !string.IsNullOrEmpty(category.EnglishName))
                {
                    categoryName = category.EnglishName;
                }

                decimal categorySubtotal = 0m;
                var tasksForCategory = new List<object>();

                foreach (var task in calc.Tasks.OrderBy(t => t.SequenceOrder))
                {
                    decimal effectivePrice = task.EstimatedPrice;
                    categorySubtotal += effectivePrice;
                    tasksForCategory.Add(new
                    {
                        Description = task.Title,
                        Amount = effectivePrice.ToString("N2"),
                        AcceptanceCriteria = task.AcceptanceCriteria?.Select(c => localizeCriteria(c.Description)).ToList() ?? new List<string>()
                    });
                }

                grandTotal += categorySubtotal;

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

                if (jp.ServiceCategory != null && !isBg && !string.IsNullOrEmpty(jp.ServiceCategory.EnglishName) && categoryName == jp.ServiceCategory.Name)
                {
                    categoryName = jp.ServiceCategory.EnglishName;
                }

                decimal subtotal = 0m;
                var tasksForCategory = new List<object>();

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
                    tasksForCategory.Add(new
                    {
                        Description = task.Title,
                        Amount = effectivePrice.ToString("N2"),
                        AcceptanceCriteria = task.AcceptanceCriteria?.Select(c => localizeCriteria(c.Description)).ToList() ?? new List<string>()
                    });
                }

                grandTotal += subtotal;

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
                SubtotalLabel = string.Format(_localizer["Label_Subtotal"].Value, kvp.Key),
                Tasks = kvp.Value.Tasks
            });
        }

        var homeowner = await _unitOfWork.Users.GetByIdAsync(project.HomeownerId);
        var clientName = homeowner != null ? $"{homeowner.FirstName} {homeowner.LastName}" : "Valued Client";
        var clientAddress = project.JobPosts?.FirstOrDefault()?.Location ?? homeowner?.Location ?? "TBD";

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
        if (combinedScope.Length > 0)
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
        bool showPageBreakAfterPricing = true;

        string briefClass = "pdf-page-flow";
        string pricingClass = "pdf-page-flow";
        string termsClass = "pdf-page-fixed";

        var offerData = new
        {
            Header_Hello = _localizer["Header_Hello"].Value,
            Header_PreparedBy = _localizer["Header_PreparedBy"].Value,
            Header_ProjectProposal = _localizer["Header_ProjectProposal"].Value,
            Header_Overview = _localizer["Header_Overview"].Value,
            Header_PreparedFor = _localizer["Header_PreparedFor"].Value,
            Header_Fees = _localizer["Header_Fees"].Value,
            Label_FeesDescription = _localizer["Label_FeesDescription"].Value,
            Label_GrandTotal = _localizer["Label_GrandTotal"].Value,
            Header_Terms = _localizer["Header_Terms"].Value,

            Terms_Intro = _localizer["Terms_Intro"].Value,
            Terms_Point1 = _localizer["Terms_Point1"].Value,
            Terms_Point2 = _localizer["Terms_Point2"].Value,
            Terms_Point3 = _localizer["Terms_Point3"].Value,
            Terms_Point4 = _localizer["Terms_Point4"].Value,
            Terms_Point5 = _localizer["Terms_Point5"].Value,

            Footer_Validity = _localizer["Footer_Validity"].Value,
            Label_ProjectBrief = _localizer["Label_ProjectBrief"].Value,
            Label_PricingBreakdown = _localizer["Label_PricingBreakdown"].Value,
            Label_TC = _localizer["Label_TC"].Value,

            CurrencySymbol = currencySymbol,

            JobTitle = project.Title,
            JobId = project.Id.ToString().Substring(0, 8),
            TradesmanName = _localizer["Label_SystemEstimate"].Value,
            Date = System.Globalization.CultureInfo.CurrentCulture.Name.StartsWith("bg", StringComparison.OrdinalIgnoreCase)
                ? DateTime.UtcNow.ToString("dd.MM.yyyy")
                : DateTime.UtcNow.ToString("MMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture),
            ClientName = clientName,
            ClientAddress = clientAddress,
            ScopeDescription = finalScopeDescription,
            Categories = categoriesData,
            SubtotalAmount = grandTotal.ToString("N2"),
            TotalAmount = grandTotal.ToString("N2"),
            ShowPageBreakAfterBrief = showPageBreakAfterBrief,
            ShowPageBreakAfterPricing = showPageBreakAfterPricing,
            BriefClass = briefClass,
            PricingClass = pricingClass,
            TermsClass = termsClass
        };

        byte[] pdfBytes = await _pdfGeneratorService.GenerateOfferPdfAsync(offerData);

        var activeCategoryIds = activeJobPosts.Select(j => j.ServiceCategoryId).Distinct().ToList();
        var pricedCategoryIds = projectCalcs.Where(c => c.Tasks != null && c.Tasks.Any()).Select(c => c.ServiceCategoryId).Distinct().ToList();
        bool allActiveCategoriesArePriced = activeCategoryIds.Any()
            ? activeCategoryIds.All(id => pricedCategoryIds.Contains(id))
            : (hasValidCalcs || (activeJobPosts.Any() && activeJobPosts.All(jp => jp.JobTasks != null && jp.JobTasks.Any())));

        if (allActiveCategoriesArePriced)
        {
            project.MasterOfferPdf = pdfBytes;
            project.GeneralSummary = finalScopeDescription;
            project.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Projects.Update(project);
            await _unitOfWork.SaveChangesAsync();
        }

        return File(pdfBytes, "application/pdf", $"{project.Title}_Offer.pdf");
    }
}

