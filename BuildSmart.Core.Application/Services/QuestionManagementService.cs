using BuildSmart.Core.Application.DTOs;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BuildSmart.Core.Application.Services;

public class QuestionManagementService : IQuestionManagementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPricingEngine _pricingEngine;
    private readonly ILogger<QuestionManagementService> _logger;

    public QuestionManagementService(
        IUnitOfWork unitOfWork,
        IPricingEngine pricingEngine,
        ILogger<QuestionManagementService> logger)
    {
        _unitOfWork = unitOfWork;
        _pricingEngine = pricingEngine;
        _logger = logger;
    }

    #region Questions

    public async Task<Question> CreateQuestionAsync(Question question, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.Questions.AddAsync(question);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await SyncCategoryTemplateAsync(question.ServiceCategoryId, cancellationToken);
        return question;
    }

    public async Task<Question> UpdateQuestionAsync(Question question, CancellationToken cancellationToken = default)
    {
        _unitOfWork.Questions.Update(question);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await SyncCategoryTemplateAsync(question.ServiceCategoryId, cancellationToken);
        return question;
    }

    public async Task<Question> UpdateQuestionLinksAsync(Guid questionId, List<Guid> skuIds, List<Guid> formulaIds, CancellationToken cancellationToken = default)
    {
        var question = await _unitOfWork.Questions.GetByIdAsync(questionId);
        if (question == null)
        {
            throw new ArgumentException($"Question with ID {questionId} not found.");
        }

        question.SkuIds = skuIds;
        question.FormulaIds = formulaIds;
        question.UpdatedAt = DateTime.UtcNow;

        // Sync many-to-many navigation properties in EF Core
        question.Skus.Clear();
        foreach (var skuId in skuIds)
        {
            var sku = await _unitOfWork.ServiceSkus.GetByIdAsync(skuId);
            if (sku != null)
            {
                question.Skus.Add(sku);
            }
        }

        question.Formulas.Clear();
        foreach (var formulaId in formulaIds)
        {
            var formula = await _unitOfWork.Formulas.GetByIdAsync(formulaId);
            if (formula != null)
            {
                question.Formulas.Add(formula);
            }
        }

        _unitOfWork.Questions.Update(question);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await SyncCategoryTemplateAsync(question.ServiceCategoryId, cancellationToken);

        return question;
    }

    public async Task DeleteQuestionAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        var question = await _unitOfWork.Questions.GetByIdAsync(questionId);
        if (question != null)
        {
            var categoryId = question.ServiceCategoryId;
            _unitOfWork.Questions.Delete(question);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await SyncCategoryTemplateAsync(categoryId, cancellationToken);
        }
    }

    public async Task<Question?> GetQuestionByIdAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Questions.GetByIdAsync(questionId);
    }

    public async Task<IEnumerable<Question>> GetAllQuestionsAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Questions.GetAllAsync();
    }

    #endregion

    #region SKUs

    public async Task<ServiceSku> CreateSkuAsync(ServiceSku sku, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.ServiceSkus.AddAsync(sku);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return sku;
    }

    public async Task<ServiceSku> UpdateSkuAsync(ServiceSku sku, CancellationToken cancellationToken = default)
    {
        _unitOfWork.ServiceSkus.Update(sku);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return sku;
    }

    public async Task DeleteSkuAsync(Guid skuId, CancellationToken cancellationToken = default)
    {
        var sku = await _unitOfWork.ServiceSkus.GetByIdAsync(skuId);
        if (sku != null)
        {
            _unitOfWork.ServiceSkus.Delete(sku);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    #endregion

    #region Formulas

    public async Task<Formula> CreateFormulaAsync(Formula formula, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.Formulas.AddAsync(formula);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return formula;
    }

    public async Task<Formula> UpdateFormulaAsync(Formula formula, CancellationToken cancellationToken = default)
    {
        _unitOfWork.Formulas.Update(formula);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return formula;
    }

    public async Task DeleteFormulaAsync(Guid formulaId, CancellationToken cancellationToken = default)
    {
        var formula = await _unitOfWork.Formulas.GetByIdAsync(formulaId);
        if (formula != null)
        {
            _unitOfWork.Formulas.Delete(formula);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<Formula>> GetAllFormulasAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Formulas.GetAllAsync();
    }

    #endregion

    #region Graph Data

    public async Task<(IEnumerable<GraphNodeDto> Nodes, IEnumerable<GraphEdgeDto> Edges)> GetGraphDataAsync(CancellationToken cancellationToken = default)
    {
        var questions = await _unitOfWork.Questions.GetAllAsync();
        var formulas = await _unitOfWork.Formulas.GetAllAsync();

        
        var categoryList = await GetAllCategoriesInternalAsync(cancellationToken);
        var skuList = new List<ServiceSku>();
        foreach (var cat in categoryList)
        {
            var skus = await _unitOfWork.ServiceSkus.GetByCategoryAsync(cat.Id);
            skuList.AddRange(skus);
        }

        var nodes = new List<GraphNodeDto>();
        var edges = new List<GraphEdgeDto>();

        // 1. Add Questions
        foreach (var q in questions)
        {
            var catName = categoryList.FirstOrDefault(c => c.Id == q.ServiceCategoryId)?.Name ?? (q.ServiceCategoryId == null ? "Global" : "Unknown");
            nodes.Add(new GraphNodeDto
            {
                Id = q.Id.ToString(),
                Label = q.QuestionCode,
                Type = "question",
                Category = catName
            });

            // Parent Question edge
            if (q.ParentQuestionId.HasValue)
            {
                edges.Add(new GraphEdgeDto
                {
                    From = q.ParentQuestionId.Value.ToString(),
                    To = q.Id.ToString(),
                    Type = "question-to-question",
                    Label = string.IsNullOrEmpty(q.VisibilityCondition) ? null : q.VisibilityCondition
                });
            }

            // Sku linkages
            foreach (var skuId in q.SkuIds)
            {
                var sku = skuList.FirstOrDefault(s => s.Id == skuId);
                string? edgeLabel = null;
                if (sku != null && !string.IsNullOrEmpty(sku.CalculationFormula))
                {
                    var formula = sku.CalculationFormula;
                    if (formula.Contains(q.QuestionCode))
                    {
                        if (formula.StartsWith("if(", StringComparison.OrdinalIgnoreCase))
                        {
                            int firstComma = FindFirstTopLevelComma(formula, 3);
                            if (firstComma > 3)
                            {
                                var conditionPart = formula.Substring(3, firstComma - 3).Trim();
                                edgeLabel = CleanConditionLabel(conditionPart, q.QuestionCode);
                            }
                        }
                    }
                }

                edges.Add(new GraphEdgeDto
                {
                    From = q.Id.ToString(),
                    To = skuId.ToString(),
                    Type = "question-to-sku",
                    Label = edgeLabel
                });
            }

            // Formula linkages
            foreach (var formulaId in q.FormulaIds)
            {
                edges.Add(new GraphEdgeDto
                {
                    From = q.Id.ToString(),
                    To = formulaId.ToString(),
                    Type = "question-to-formula"
                });
            }
        }

        // 2. Add Formulas
        foreach (var f in formulas)
        {
            nodes.Add(new GraphNodeDto
            {
                Id = f.Id.ToString(),
                Label = f.Name,
                Type = "formula",
                Category = "Pricing Formula"
            });
        }

        // 3. Add SKUs
        foreach (var s in skuList)
        {
            var catName = categoryList.FirstOrDefault(c => c.Id == s.ServiceCategoryId)?.Name ?? "Unknown";
            nodes.Add(new GraphNodeDto
            {
                Id = s.Id.ToString(),
                Label = s.SkuCode,
                Type = "sku",
                Category = catName
            });

            // If Sku calculation formula matches any formula name, add edge Formula -> Sku
            foreach (var f in formulas)
            {
                if (s.CalculationFormula.Contains(f.Name))
                {
                    edges.Add(new GraphEdgeDto
                    {
                        From = f.Id.ToString(),
                        To = s.Id.ToString(),
                        Type = "formula-to-sku"
                    });
                }
            }
        }

        return (nodes, edges);
    }

    private int FindFirstTopLevelComma(string str, int startIndex)
    {
        int parenDepth = 0;
        for (int i = startIndex; i < str.Length; i++)
        {
            char c = str[i];
            if (c == '(') parenDepth++;
            else if (c == ')') parenDepth--;
            else if (c == ',' && parenDepth == 0)
            {
                return i;
            }
        }
        return -1;
    }

    private string CleanConditionLabel(string condition, string questionCode)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(
            condition, 
            @"Contains\(\s*" + System.Text.RegularExpressions.Regex.Escape(questionCode) + @"\s*,\s*['""]?(.*?)['""]?\s*\)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

        if (matches.Count > 0)
        {
            var values = matches.Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value).ToList();
            return string.Join(" | ", values);
        }
        
        var eqMatch = System.Text.RegularExpressions.Regex.Match(condition, System.Text.RegularExpressions.Regex.Escape(questionCode) + @"\s*==\s*['""]?(.*?)['""]?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (eqMatch.Success)
        {
            return eqMatch.Groups[1].Value;
        }

        return condition.Replace(questionCode, "Answer").Trim();
    }

    #endregion

    #region Import/Export

    public async Task<string> ExportSpiderNetAsync(CancellationToken cancellationToken = default)
    {
        var questions = await _unitOfWork.Questions.GetAllAsync();
        var formulas = await _unitOfWork.Formulas.GetAllAsync();
        var categories = await GetAllCategoriesInternalAsync(cancellationToken);
        var skus = await _unitOfWork.ServiceSkus.GetAllAsync();

        var data = new
        {
            ExportedAt = DateTime.UtcNow,
            Categories = categories.Select(c => new { c.Id, c.Name, c.IsGlobal }),
            Skus = skus.Select(s => new { s.Id, s.SkuCode }),
            Questions = questions.Select(q => new
            {
                q.Id,
                q.QuestionCode,
                q.Text,
                q.Type,
                q.IsRequired,
                q.OptionsJson,
                q.HintText,
                q.ServiceCategoryId,
                q.ParentQuestionId,
                q.DisplayOrder,
                q.VisibilityCondition,
                q.SkuIds,
                q.FormulaIds
            }),
            Formulas = formulas.Select(f => new
            {
                f.Id,
                f.Name,
                f.Description,
                f.Expression
            })
        };

        return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task ImportSpiderNetAsync(string jsonContent, CancellationToken cancellationToken = default)
    {
        using var doc = JsonDocument.Parse(jsonContent);
        var root = doc.RootElement;

        // 1. Build category ID mapping (exportedId -> liveId)
        var dbCategories = await _unitOfWork.ServiceCategories.GetAllAsync();
        var exportedCategoryIds = new Dictionary<Guid, Guid>();
        if (root.TryGetProperty("Categories", out var categoriesArr))
        {
            foreach (var cJson in categoriesArr.EnumerateArray())
            {
                var expId = cJson.GetProperty("Id").GetGuid();
                var name = cJson.GetProperty("Name").GetString();
                if (!string.IsNullOrEmpty(name))
                {
                    var liveCat = dbCategories.FirstOrDefault(c => c.Name == name);
                    if (liveCat != null)
                    {
                        exportedCategoryIds[expId] = liveCat.Id;
                    }
                }
            }
        }

        // 2. Build SKU ID mapping (exportedId -> liveId)
        var dbSkus = await _unitOfWork.ServiceSkus.GetAllAsync();
        var exportedSkuIds = new Dictionary<Guid, Guid>();
        if (root.TryGetProperty("Skus", out var skusArr))
        {
            foreach (var sJson in skusArr.EnumerateArray())
            {
                var expId = sJson.GetProperty("Id").GetGuid();
                var code = sJson.GetProperty("SkuCode").GetString();
                if (!string.IsNullOrEmpty(code))
                {
                    var liveSku = dbSkus.FirstOrDefault(s => s.SkuCode == code);
                    if (liveSku != null)
                    {
                        exportedSkuIds[expId] = liveSku.Id;
                    }
                }
            }
        }

        // Import Formulas
        if (root.TryGetProperty("Formulas", out var formulasArr))
        {
            foreach (var fJson in formulasArr.EnumerateArray())
            {
                var id = fJson.GetProperty("Id").GetGuid();
                var name = fJson.GetProperty("Name").GetString() ?? string.Empty;
                var desc = fJson.GetProperty("Description").GetString() ?? string.Empty;
                var expr = fJson.GetProperty("Expression").GetString() ?? string.Empty;

                var existing = await _unitOfWork.Formulas.GetByIdAsync(id);
                if (existing != null)
                {
                    existing.Name = name;
                    existing.Description = desc;
                    existing.Expression = expr;
                    existing.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Formulas.Update(existing);
                }
                else
                {
                    var formula = new Formula
                    {
                        Id = id,
                        Name = name,
                        Description = desc,
                        Expression = expr,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Formulas.AddAsync(formula);
                }
            }
        }

        // Import Questions
        if (root.TryGetProperty("Questions", out var questionsArr))
        {
            foreach (var qJson in questionsArr.EnumerateArray())
            {
                var id = qJson.GetProperty("Id").GetGuid();
                var code = qJson.GetProperty("QuestionCode").GetString() ?? string.Empty;
                var text = qJson.GetProperty("Text").GetString() ?? string.Empty;
                var type = qJson.GetProperty("Type").GetString() ?? string.Empty;
                var required = qJson.GetProperty("IsRequired").GetBoolean();
                var options = qJson.TryGetProperty("OptionsJson", out var oProp) ? oProp.GetString() : null;
                var hint = qJson.TryGetProperty("HintText", out var hProp) ? hProp.GetString() : null;
                var categoryId = qJson.TryGetProperty("ServiceCategoryId", out var cProp) && cProp.ValueKind != JsonValueKind.Null ? (Guid?)cProp.GetGuid() : null;
                var parentId = qJson.TryGetProperty("ParentQuestionId", out var pProp) && pProp.ValueKind != JsonValueKind.Null ? (Guid?)pProp.GetGuid() : null;
                var order = qJson.GetProperty("DisplayOrder").GetInt32();
                var condition = qJson.TryGetProperty("VisibilityCondition", out var vProp) ? vProp.GetString() : null;

                // Resolve live Category ID
                Guid? mappedCategoryId = null;
                if (categoryId.HasValue && exportedCategoryIds.TryGetValue(categoryId.Value, out var liveCid))
                {
                    mappedCategoryId = liveCid;
                }

                // Resolve live SKU IDs
                var skuIds = new List<Guid>();
                if (qJson.TryGetProperty("SkuIds", out var skusJsonArr))
                {
                    foreach (var sVal in skusJsonArr.EnumerateArray())
                    {
                        var expSkuId = sVal.GetGuid();
                        if (exportedSkuIds.TryGetValue(expSkuId, out var liveSkuId))
                        {
                            skuIds.Add(liveSkuId);
                        }
                    }
                }

                var formulaIds = new List<Guid>();
                if (qJson.TryGetProperty("FormulaIds", out var formulasList))
                {
                    formulaIds = formulasList.EnumerateArray().Select(f => f.GetGuid()).ToList();
                }

                var existing = await _unitOfWork.Questions.GetByIdAsync(id);
                Question targetQuestion;

                if (existing != null)
                {
                    existing.QuestionCode = code;
                    existing.Text = text;
                    existing.Type = type;
                    existing.IsRequired = required;
                    existing.OptionsJson = options;
                    existing.HintText = hint;
                    existing.ServiceCategoryId = mappedCategoryId;
                    existing.ParentQuestionId = parentId;
                    existing.DisplayOrder = order;
                    existing.VisibilityCondition = condition;
                    existing.SkuIds = skuIds;
                    existing.FormulaIds = formulaIds;
                    existing.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Questions.Update(existing);
                    targetQuestion = existing;
                }
                else
                {
                    var question = new Question
                    {
                        Id = id,
                        QuestionCode = code,
                        Text = text,
                        Type = type,
                        IsRequired = required,
                        OptionsJson = options,
                        HintText = hint,
                        ServiceCategoryId = mappedCategoryId,
                        ParentQuestionId = parentId,
                        DisplayOrder = order,
                        VisibilityCondition = condition,
                        SkuIds = skuIds,
                        FormulaIds = formulaIds,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Questions.AddAsync(question);
                    targetQuestion = question;
                }

                // Explicitly sync many-to-many relationship with live SKU instances in context
                targetQuestion.Skus.Clear();
                foreach (var skuId in skuIds)
                {
                    var sku = dbSkus.FirstOrDefault(s => s.Id == skuId);
                    if (sku != null)
                    {
                        targetQuestion.Skus.Add(sku);
                    }
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Sync all categories templates
        var allCategoryIds = (await _unitOfWork.Questions.GetAllAsync())
            .Select(q => q.ServiceCategoryId)
            .Where(cid => cid.HasValue)
            .Select(cid => cid!.Value)
            .Distinct();

        foreach (var catId in allCategoryIds)
        {
            await SyncCategoryTemplateAsync(catId, cancellationToken);
        }
    }

    #endregion

    #region Offer Simulation

    public async Task<OfferSimulationResultDto> ExecuteOfferSimulationAsync(List<Guid> selectedQuestionIds, string jobDetailsJson, CancellationToken cancellationToken = default)
    {
        var result = new OfferSimulationResultDto();
        var questions = await _unitOfWork.Questions.GetAllAsync();
        var formulas = await _unitOfWork.Formulas.GetAllAsync();
        var categories = await GetAllCategoriesInternalAsync(cancellationToken);

        // Parse answers dictionary
        var answers = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(jobDetailsJson))
        {
            using var doc = JsonDocument.Parse(jobDetailsJson);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Number)
                    answers[prop.Name] = prop.Value.GetDecimal();
                else if (prop.Value.ValueKind == JsonValueKind.String)
                    answers[prop.Name] = prop.Value.GetString() ?? string.Empty;
                else if (prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False)
                    answers[prop.Name] = prop.Value.GetBoolean();
                else if (prop.Value.ValueKind == JsonValueKind.Array)
                    answers[prop.Name] = prop.Value.GetRawText();
            }
        }

        // 1. Evaluate Reusable Formulas first
        // We add calculated formulas to answers context so SKUs can reference them
        foreach (var formula in formulas)
        {
            try
            {
                var val = _pricingEngine.CalculateQuantity(formula.Expression, JsonSerializer.Serialize(answers));
                answers[formula.Name] = val;
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Formula '{formula.Name}' evaluation failed: {ex.Message}");
            }
        }

        // 2. Fetch SKUs for categories linked to selected questions
        var activeCategoryIds = questions
            .Where(q => selectedQuestionIds.Contains(q.Id) && q.ServiceCategoryId.HasValue)
            .Select(q => q.ServiceCategoryId!.Value)
            .Distinct()
            .ToList();

        // Always include global SKUs if any exist
        var globalCategories = categories.Where(c => c.IsGlobal).Select(c => c.Id).ToList();
        activeCategoryIds.AddRange(globalCategories);
        activeCategoryIds = activeCategoryIds.Distinct().ToList();

        var skus = new List<ServiceSku>();
        foreach (var categoryId in activeCategoryIds)
        {
            var categorySkus = await _unitOfWork.ServiceSkus.GetByCategoryAsync(categoryId);
            skus.AddRange(categorySkus);
        }

        // 3. Evaluate SKU pricing
        var updatedJobDetails = JsonSerializer.Serialize(answers);
        foreach (var sku in skus)
        {
            if (string.IsNullOrWhiteSpace(sku.CalculationFormula)) continue;

            try
            {
                var qty = _pricingEngine.CalculateQuantity(sku.CalculationFormula, updatedJobDetails);
                if (qty > 0)
                {
                    var basePrice = sku.BasePrice;
                    var totalPrice = basePrice * qty;

                    var catName = categories.FirstOrDefault(c => c.Id == sku.ServiceCategoryId)?.Name ?? "Unknown";

                    result.Tasks.Add(new CalculatedTaskDto
                    {
                        SkuCode = sku.SkuCode,
                        Title = sku.Name,
                        Quantity = qty,
                        UnitType = sku.UnitType,
                        BasePrice = basePrice,
                        TotalPrice = totalPrice
                    });

                    result.SkuQuantities[sku.SkuCode] = qty;

                    if (result.PriceBreakdown.ContainsKey(catName))
                    {
                        result.PriceBreakdown[catName] += totalPrice;
                    }
                    else
                    {
                        result.PriceBreakdown[catName] = totalPrice;
                    }

                    result.TotalPrice += totalPrice;
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"SKU '{sku.SkuCode}' quantity calculation failed: {ex.Message}");
            }
        }

        return result;
    }

    #endregion

    #region Helpers

    private async Task<List<ServiceCategory>> GetAllCategoriesInternalAsync(CancellationToken cancellationToken)
    {
        // Simple fallback to query categories using UnitOfWork.ServiceCategories
        // We know from UnitOfWork.cs that ServiceCategories is IServiceCategoryRepository
        // Let's call a method or use a query if IServiceCategoryRepository doesn't expose GetAll.
        // Let's see what is inside IServiceCategoryRepository.cs.
        // It has GetByIdAsync, but we can query all categories.
        // Wait, does it have a GetActiveAsync or similar?
        // Let's look at IServiceCategoryRepository.cs!
        return (await _unitOfWork.ServiceCategories.GetAllAsync()).ToList();
    }

    private async Task SyncCategoryTemplateAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        if (categoryId == null || categoryId == Guid.Empty) return;

        var category = await _unitOfWork.ServiceCategories.GetByIdAsync(categoryId.Value);
        if (category == null) return;

        var questions = (await _unitOfWork.Questions.GetAllAsync())
            .Where(q => q.ServiceCategoryId == categoryId)
            .OrderBy(q => q.DisplayOrder)
            .ToList();

        var allQuestions = await _unitOfWork.Questions.GetAllAsync();
        var templateQuestions = questions.Select(q => new
        {
            id = q.QuestionCode,
            text = q.Text,
            type = q.Type,
            required = q.IsRequired,
            options = string.IsNullOrWhiteSpace(q.OptionsJson) 
                ? null 
                : JsonSerializer.Deserialize<List<string>>(q.OptionsJson),
            hintText = q.HintText,
            dependsOn = allQuestions.FirstOrDefault(pq => pq.Id == q.ParentQuestionId)?.QuestionCode,
            dependsOnValue = q.VisibilityCondition
        }).ToList();

        var template = new { questions = templateQuestions };
        category.TemplateStructure = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
        category.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.ServiceCategories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    #endregion
}
