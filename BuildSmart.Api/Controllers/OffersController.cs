using Microsoft.AspNetCore.Mvc;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Application.Resources;
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
    public async Task<IActionResult> DownloadOfferPdf(Guid projectId)
    {
        var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
        if (project == null)
        {
            return NotFound("Project not found.");
        }

        if (project.JobPosts.Any(jp => jp.Status == JobPostStatus.Draft))
        {
            return BadRequest("Cannot download offer until all categories are filled out.");
        }

        if (project.MasterOfferPdf == null || project.MasterOfferPdf.Length == 0)
        {
            // Dynamically generate the PDF
            var projectCalcs = (await _unitOfWork.AiCalculations.GetByProjectWithTasksAsync(projectId)).ToList();
            if (projectCalcs.Count == 0)
            {
                return BadRequest("No categories have been priced yet for this project. Please run calculations first.");
            }

            string currencySymbol = "€";
            decimal grandTotal = 0;
            var categoriesData = new List<object>();

            foreach (var calc in projectCalcs)
            {
                grandTotal += calc.TotalEstimatedPrice;
                var category = await _unitOfWork.ServiceCategories.GetByIdAsync(calc.ServiceCategoryId);
                var categoryName = category?.Name ?? "General";
                if (category != null)
                {
                    var targetLang = project.LanguageCode ?? "bg";
                    if (targetLang.StartsWith("en", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(category.EnglishName))
                    {
                        categoryName = category.EnglishName;
                    }
                }

                var tasksForCategory = calc.Tasks.OrderBy(t => t.SequenceOrder).Select(task => new
                {
                    Description = task.Title,
                    Amount = task.EstimatedPrice.ToString("N2"),
                    AcceptanceCriteria = task.AcceptanceCriteria?.Select(c => c.Description).ToList() ?? new List<string>()
                }).ToList();

                categoriesData.Add(new
                {
                    CategoryName = categoryName,
                    Subtotal = calc.TotalEstimatedPrice.ToString("N2"),
                    SubtotalLabel = string.Format(_localizer["Label_Subtotal"].Value, categoryName),
                    Tasks = tasksForCategory
                });
            }

            var homeowner = await _unitOfWork.Users.GetByIdAsync(project.HomeownerId);
            var clientName = homeowner != null ? $"{homeowner.FirstName} {homeowner.LastName}" : "Valued Client";
            var clientAddress = project.JobPosts.FirstOrDefault()?.Location ?? homeowner?.Location ?? "TBD";

            var combinedScope = new StringBuilder();
            foreach (var jp in project.JobPosts.Where(j => !string.IsNullOrWhiteSpace(j.GeneratedScope)))
            {
                combinedScope.AppendLine($"## {jp.Title}");
                combinedScope.AppendLine(jp.GeneratedScope);
                combinedScope.AppendLine();
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
                    // Fallback to default description if AI generation fails or rate-limits
                }
            }

            // Page Squashing and Space Unit (SU) Calculation
            int suBrief = 15 + (finalScopeDescription?.Length ?? 0) / 150;
            int suPricing = 10;
            foreach (var calc in projectCalcs)
            {
                suPricing += 5; // 5 SU per category header + subtotal
                suPricing += calc.Tasks.Count * 3; // 3 SU per task
                foreach (var task in calc.Tasks)
                {
                    if (task.AcceptanceCriteria != null)
                    {
                        suPricing += task.AcceptanceCriteria.Count; // 1 SU per acceptance criteria bullet point
                    }
                }
            }
            int suTerms = 25; // T&C takes about 25 SU

            // Page break decisions
            bool showPageBreakAfterBrief = true;
            bool showPageBreakAfterPricing = true;
            bool isBriefSinglePage = suBrief <= 15; // Max ~1,500 chars to guarantee single-page safety
            bool isPricingSinglePage = suPricing <= 25; // Guarantee safety margin for pricing tables

            if (suBrief + suPricing <= 25)
            {
                // Consolidate Brief and Pricing onto the same page
                showPageBreakAfterBrief = false;
            }

            string briefClass = (showPageBreakAfterBrief && isBriefSinglePage) ? "pdf-page-fixed" : "pdf-page-flow";
            string pricingClass = (showPageBreakAfterPricing && isPricingSinglePage && showPageBreakAfterBrief) ? "pdf-page-fixed" : "pdf-page-flow";
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

            var activeJobPosts = project.JobPosts.Where(jp => jp.Status != JobPostStatus.Cancelled).ToList();
            bool allPriced = activeJobPosts.All(jp => projectCalcs.Any(c => c.ServiceCategoryId == jp.ServiceCategoryId));

            if (allPriced)
            {
                project.MasterOfferPdf = pdfBytes;
                project.GeneralSummary = finalScopeDescription;
                project.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Projects.Update(project);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                return File(pdfBytes, "application/pdf", $"{project.Title}_Offer.pdf");
            }
        }

        return File(project.MasterOfferPdf, "application/pdf", $"{project.Title}_Offer.pdf");
    }
}

