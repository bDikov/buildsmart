using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BuildSmart.SharedUI.Services;

public class QuestionDto
{
    public Guid Id { get; set; }
    public string QuestionCode { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? OptionsJson { get; set; }
    public string? HintText { get; set; }
    public Guid? ServiceCategoryId { get; set; }
    public Guid? ParentQuestionId { get; set; }
    public int DisplayOrder { get; set; }
    public string? VisibilityCondition { get; set; }
    public List<Guid> SkuIds { get; set; } = new();
    public List<Guid> FormulaIds { get; set; } = new();
    
    public string? EnglishText { get; set; }
    public string? EnglishHint { get; set; }
}

public class FormulaDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
}

public class ServiceCategoryTranslationDto
{
    public string LanguageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class ServiceCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsGlobal { get; set; }
    public string TemplateStructure { get; set; } = "{}";
    public string? Status { get; set; }
    public List<ServiceCategoryTranslationDto> Translations { get; set; } = new();
}

public class ServiceSkuTranslationDto
{
    public string LanguageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ServiceSkuDto
{
    public Guid Id { get; set; }
    public string SkuCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string UnitType { get; set; } = string.Empty;
    public Guid ServiceCategoryId { get; set; }
    public string CalculationFormula { get; set; } = string.Empty;
    
    public string? EnglishName { get; set; }
    public string? EnglishDescription { get; set; }
    public List<ServiceSkuTranslationDto> Translations { get; set; } = new();
}


public class GraphNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class GraphEdgeDto
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Label { get; set; }
}

public class QuestionGraphDto
{
    public List<GraphNodeDto> Nodes { get; set; } = new();
    public List<GraphEdgeDto> Edges { get; set; } = new();
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

public class ImportResultDto
{
    public bool Success { get; set; }
    public List<string> LogLines { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public interface IQuestionManagementApiClient
{
    Task<List<QuestionDto>> GetQuestionsAsync();
    Task<List<FormulaDto>> GetFormulasAsync();
    Task<QuestionGraphDto> GetQuestionGraphAsync();
    Task<QuestionDto> CreateQuestionAsync(QuestionDto input, string? englishText = null, string? englishHint = null);
    Task<QuestionDto> UpdateQuestionAsync(QuestionDto input, string? englishText = null, string? englishHint = null);
    Task<QuestionDto> UpdateQuestionLinksAsync(Guid questionId, List<Guid> skuIds, List<Guid> formulaIds);
    Task<bool> DeleteQuestionAsync(Guid questionId);
    Task<FormulaDto> CreateFormulaAsync(FormulaDto input);
    Task<FormulaDto> UpdateFormulaAsync(FormulaDto input);
    Task<bool> DeleteFormulaAsync(Guid formulaId);
    Task<OfferSimulationResultDto> RunOfferSimulationAsync(List<Guid> selectedQuestionIds, string jobDetailsJson);
    Task<List<ServiceCategoryDto>> GetServiceCategoriesAsync();
    Task<ServiceCategoryDto> SaveCategoryAsync(ServiceCategoryDto input, string? englishName);
    Task<bool> DeleteServiceCategoryAsync(Guid id);
    Task<ServiceSkuDto> CreateServiceSkuAsync(ServiceSkuDto input, string? englishName = null, string? englishDescription = null);
    Task<ServiceSkuDto> UpdateServiceSkuAsync(ServiceSkuDto input, string? englishName = null, string? englishDescription = null);
    Task<bool> DeleteServiceSkuAsync(Guid id);
    Task<List<ServiceSkuDto>> GetServiceSkusByCategoryAsync(Guid categoryId);
    Task<ImportResultDto> ImportSpiderNetConfigAsync(string json);
    Task<Dictionary<string, string>> GetEnglishTranslationsAsync();
    Task<bool> UpdateTranslationAsync(string key, string culture, string value);
}

public class QuestionManagementApiClient : IQuestionManagementApiClient
{
    private readonly HttpClient _httpClient;

    public QuestionManagementApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private async Task<JsonElement> SendQueryAsync(string query, object? variables = null)
    {
        var requestBody = new
        {
            query = query,
            variables = variables
        };

        var response = await _httpClient.PostAsJsonAsync(ApiConfig.GetGraphQLUrl(), requestBody);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (result.TryGetProperty("errors", out var errorsProp))
        {
            throw new Exception($"GraphQL Error: {errorsProp.GetRawText()}");
        }

        return result.GetProperty("data");
    }

    public async Task<List<QuestionDto>> GetQuestionsAsync()
    {
        var query = @"
            query {
                questions {
                    id
                    questionCode
                    text
                    type
                    isRequired
                    optionsJson
                    hintText
                    serviceCategoryId
                    parentQuestionId
                    displayOrder
                    visibilityCondition
                    skuIds
                    formulaIds
                    englishText
                    englishHint
                }
            }";

        var data = await SendQueryAsync(query);
        var questionsJson = data.GetProperty("questions").GetRawText();
        return JsonSerializer.Deserialize<List<QuestionDto>>(questionsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
    }

    public async Task<List<FormulaDto>> GetFormulasAsync()
    {
        var query = @"
            query {
                formulas {
                    id
                    name
                    description
                    expression
                }
            }";

        var data = await SendQueryAsync(query);
        var formulasJson = data.GetProperty("formulas").GetRawText();
        return JsonSerializer.Deserialize<List<FormulaDto>>(formulasJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
    }

    public async Task<QuestionGraphDto> GetQuestionGraphAsync()
    {
        var query = @"
            query {
                questionGraph {
                    nodes {
                        id
                        label
                        type
                        category
                    }
                    edges {
                        from
                        to
                        type
                        label
                    }
                }
            }";

        var data = await SendQueryAsync(query);
        var graphJson = data.GetProperty("questionGraph").GetRawText();
        return JsonSerializer.Deserialize<QuestionGraphDto>(graphJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
    }

    public async Task<QuestionDto> CreateQuestionAsync(QuestionDto input, string? englishText = null, string? englishHint = null)
    {
        var query = @"
            mutation($questionCode: String!, $text: String!, $type: String!, $isRequired: Boolean!, $optionsJson: String, $hintText: String, $serviceCategoryId: UUID, $parentQuestionId: UUID, $displayOrder: Int!, $visibilityCondition: String, $englishText: String, $englishHint: String) {
                createQuestion(
                    questionCode: $questionCode
                    text: $text
                    type: $type
                    isRequired: $isRequired
                    optionsJson: $optionsJson
                    hintText: $hintText
                    serviceCategoryId: $serviceCategoryId
                    parentQuestionId: $parentQuestionId
                    displayOrder: $displayOrder
                    visibilityCondition: $visibilityCondition
                    englishText: $englishText
                    englishHint: $englishHint
                ) {
                    id
                    questionCode
                    text
                    type
                    isRequired
                    optionsJson
                    hintText
                    serviceCategoryId
                    parentQuestionId
                    displayOrder
                    visibilityCondition
                    skuIds
                    formulaIds
                    englishText
                    englishHint
                }
            }";

        var variables = new
        {
            questionCode = input.QuestionCode,
            text = input.Text,
            type = input.Type,
            isRequired = input.IsRequired,
            optionsJson = input.OptionsJson,
            hintText = input.HintText,
            serviceCategoryId = input.ServiceCategoryId,
            parentQuestionId = input.ParentQuestionId,
            displayOrder = input.DisplayOrder,
            visibilityCondition = input.VisibilityCondition,
            englishText = englishText,
            englishHint = englishHint
        };

        var data = await SendQueryAsync(query, variables);
        var resultJson = data.GetProperty("createQuestion").GetRawText();
        return JsonSerializer.Deserialize<QuestionDto>(resultJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    public async Task<QuestionDto> UpdateQuestionAsync(QuestionDto input, string? englishText = null, string? englishHint = null)
    {
        var query = @"
            mutation($id: UUID!, $questionCode: String!, $text: String!, $type: String!, $isRequired: Boolean!, $optionsJson: String, $hintText: String, $serviceCategoryId: UUID, $parentQuestionId: UUID, $displayOrder: Int!, $visibilityCondition: String, $englishText: String, $englishHint: String) {
                updateQuestion(
                    id: $id
                    questionCode: $questionCode
                    text: $text
                    type: $type
                    isRequired: $isRequired
                    optionsJson: $optionsJson
                    hintText: $hintText
                    serviceCategoryId: $serviceCategoryId
                    parentQuestionId: $parentQuestionId
                    displayOrder: $displayOrder
                    visibilityCondition: $visibilityCondition
                    englishText: $englishText
                    englishHint: $englishHint
                ) {
                    id
                    questionCode
                    text
                    type
                    isRequired
                    optionsJson
                    hintText
                    serviceCategoryId
                    parentQuestionId
                    displayOrder
                    visibilityCondition
                    skuIds
                    formulaIds
                    englishText
                    englishHint
                }
            }";

        var variables = new
        {
            id = input.Id,
            questionCode = input.QuestionCode,
            text = input.Text,
            type = input.Type,
            isRequired = input.IsRequired,
            optionsJson = input.OptionsJson,
            hintText = input.HintText,
            serviceCategoryId = input.ServiceCategoryId,
            parentQuestionId = input.ParentQuestionId,
            displayOrder = input.DisplayOrder,
            visibilityCondition = input.VisibilityCondition,
            englishText = englishText,
            englishHint = englishHint
        };

        var data = await SendQueryAsync(query, variables);
        var resultJson = data.GetProperty("updateQuestion").GetRawText();
        return JsonSerializer.Deserialize<QuestionDto>(resultJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    public async Task<QuestionDto> UpdateQuestionLinksAsync(Guid questionId, List<Guid> skuIds, List<Guid> formulaIds)
    {
        var query = @"
            mutation($questionId: UUID!, $skuIds: [UUID!]!, $formulaIds: [UUID!]!) {
                updateQuestionLinks(
                    questionId: $questionId
                    skuIds: $skuIds
                    formulaIds: $formulaIds
                ) {
                    id
                    questionCode
                    skuIds
                    formulaIds
                }
            }";

        var variables = new
        {
            questionId = questionId,
            skuIds = skuIds,
            formulaIds = formulaIds
        };

        var data = await SendQueryAsync(query, variables);
        var resultJson = data.GetProperty("updateQuestionLinks").GetRawText();
        return JsonSerializer.Deserialize<QuestionDto>(resultJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    public async Task<bool> DeleteQuestionAsync(Guid questionId)
    {
        var query = @"
            mutation($questionId: UUID!) {
                deleteQuestion(questionId: $questionId)
            }";

        var variables = new { questionId = questionId };
        var data = await SendQueryAsync(query, variables);
        return data.GetProperty("deleteQuestion").GetBoolean();
    }

    public async Task<FormulaDto> CreateFormulaAsync(FormulaDto input)
    {
        var query = @"
            mutation($name: String!, $description: String!, $expression: String!) {
                createFormula(
                    name: $name
                    description: $description
                    expression: $expression
                ) {
                    id
                    name
                    description
                    expression
                }
            }";

        var variables = new
        {
            name = input.Name,
            description = input.Description,
            expression = input.Expression
        };

        var data = await SendQueryAsync(query, variables);
        var resultJson = data.GetProperty("createFormula").GetRawText();
        return JsonSerializer.Deserialize<FormulaDto>(resultJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    public async Task<FormulaDto> UpdateFormulaAsync(FormulaDto input)
    {
        var query = @"
            mutation($id: UUID!, $name: String!, $description: String!, $expression: String!) {
                updateFormula(
                    id: $id
                    name: $name
                    description: $description
                    expression: $expression
                ) {
                    id
                    name
                    description
                    expression
                }
            }";

        var variables = new
        {
            id = input.Id,
            name = input.Name,
            description = input.Description,
            expression = input.Expression
        };

        var data = await SendQueryAsync(query, variables);
        var resultJson = data.GetProperty("updateFormula").GetRawText();
        return JsonSerializer.Deserialize<FormulaDto>(resultJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    public async Task<bool> DeleteFormulaAsync(Guid formulaId)
    {
        var query = @"
            mutation($id: UUID!) {
                deleteFormula(id: $id)
            }";

        var variables = new { id = formulaId };
        var data = await SendQueryAsync(query, variables);
        return data.GetProperty("deleteFormula").GetBoolean();
    }

    private class GraphQLSimulationResultDto
    {
        public List<CalculatedTaskDto> Tasks { get; set; } = new();
        public List<GraphQLKeyValuePair> SkuQuantities { get; set; } = new();
        public List<GraphQLKeyValuePair> PriceBreakdown { get; set; } = new();
        public decimal TotalPrice { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    private class GraphQLKeyValuePair
    {
        public string Key { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }

    public async Task<OfferSimulationResultDto> RunOfferSimulationAsync(List<Guid> selectedQuestionIds, string jobDetailsJson)
    {
        var query = @"
            mutation($selectedQuestionIds: [UUID!]!, $jobDetailsJson: String!) {
                runOfferSimulation(
                    selectedQuestionIds: $selectedQuestionIds
                    jobDetailsJson: $jobDetailsJson
                ) {
                    tasks {
                        skuCode
                        title
                        quantity
                        unitType
                        basePrice
                        totalPrice
                    }
                    skuQuantities {
                        key
                        value
                    }
                    priceBreakdown {
                        key
                        value
                    }
                    totalPrice
                    warnings
                }
            }";

        var variables = new
        {
            selectedQuestionIds = selectedQuestionIds,
            jobDetailsJson = jobDetailsJson
        };

        var data = await SendQueryAsync(query, variables);
        var resultJson = data.GetProperty("runOfferSimulation").GetRawText();
        
        var rawDto = JsonSerializer.Deserialize<GraphQLSimulationResultDto>(resultJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        
        var finalResult = new OfferSimulationResultDto
        {
            Tasks = rawDto.Tasks,
            TotalPrice = rawDto.TotalPrice,
            Warnings = rawDto.Warnings,
            SkuQuantities = rawDto.SkuQuantities.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            PriceBreakdown = rawDto.PriceBreakdown.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };
        
        return finalResult;
    }

    public async Task<List<ServiceCategoryDto>> GetServiceCategoriesAsync()
    {
        var query = @"
            query {
                allServiceCategories {
                    id
                    name
                    description
                    isGlobal
                    templateStructure
                    status
                    englishName
                }
            }";

        var data = await SendQueryAsync(query);
        var resultJson = data.GetProperty("allServiceCategories").GetRawText();
        return JsonSerializer.Deserialize<List<ServiceCategoryDto>>(resultJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
    }

    public async Task<ServiceCategoryDto> SaveCategoryAsync(ServiceCategoryDto input, string? englishName)
    {
        var query = @"
            mutation($id: UUID, $name: String!, $description: String, $isGlobal: Boolean!, $templateStructure: String!, $status: CategoryStatus, $englishName: String) {
                saveCategory(
                    id: $id
                    name: $name
                    description: $description
                    isGlobal: $isGlobal
                    templateStructure: $templateStructure
                    status: $status
                    englishName: $englishName
                ) {
                    id
                    name
                    description
                    isGlobal
                    templateStructure
                    status
                    englishName
                }
            }";

        var variables = new
        {
            id = input.Id == Guid.Empty ? null : (Guid?)input.Id,
            name = input.Name,
            description = input.Description,
            isGlobal = input.IsGlobal,
            templateStructure = input.TemplateStructure ?? "{}",
            status = input.Status?.ToUpper() ?? "DRAFT",
            englishName = englishName
        };

        var data = await SendQueryAsync(query, variables);
        var resultJson = data.GetProperty("saveCategory").GetRawText();
        return JsonSerializer.Deserialize<ServiceCategoryDto>(resultJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    public async Task<ServiceSkuDto> CreateServiceSkuAsync(ServiceSkuDto input, string? englishName = null, string? englishDescription = null)
    {
        var query = @"
            mutation($categoryId: UUID!, $skuCode: String!, $name: String!, $description: String!, $basePrice: Decimal!, $unitType: String!, $calculationFormula: String!, $englishName: String, $englishDescription: String) {
                createServiceSku(
                    categoryId: $categoryId
                    skuCode: $skuCode
                    name: $name
                    description: $description
                    basePrice: $basePrice
                    unitType: $unitType
                    calculationFormula: $calculationFormula
                    englishName: $englishName
                    englishDescription: $englishDescription
                ) {
                    id
                    skuCode
                    name
                    description
                    basePrice
                    unitType
                    serviceCategoryId
                    calculationFormula
                    englishName
                    englishDescription
                }
            }";

        var variables = new
        {
            categoryId = input.ServiceCategoryId,
            skuCode = input.SkuCode,
            name = input.Name,
            description = input.Description ?? "",
            basePrice = input.BasePrice,
            unitType = input.UnitType ?? "",
            calculationFormula = input.CalculationFormula ?? "",
            englishName = englishName,
            englishDescription = englishDescription
        };

        var data = await SendQueryAsync(query, variables);
        var resultJson = data.GetProperty("createServiceSku").GetRawText();
        var dto = JsonSerializer.Deserialize<ServiceSkuDto>(resultJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        return dto;
    }

    public async Task<ServiceSkuDto> UpdateServiceSkuAsync(ServiceSkuDto input, string? englishName = null, string? englishDescription = null)
    {
        var query = @"
            mutation($id: UUID!, $skuCode: String!, $name: String!, $description: String!, $basePrice: Decimal!, $unitType: String!, $calculationFormula: String!, $englishName: String, $englishDescription: String) {
                updateServiceSku(
                    id: $id
                    skuCode: $skuCode
                    name: $name
                    description: $description
                    basePrice: $basePrice
                    unitType: $unitType
                    calculationFormula: $calculationFormula
                    englishName: $englishName
                    englishDescription: $englishDescription
                ) {
                    id
                    skuCode
                    name
                    description
                    basePrice
                    unitType
                    serviceCategoryId
                    calculationFormula
                    englishName
                    englishDescription
                }
            }";

        var variables = new
        {
            id = input.Id,
            skuCode = input.SkuCode,
            name = input.Name,
            description = input.Description ?? "",
            basePrice = input.BasePrice,
            unitType = input.UnitType ?? "",
            calculationFormula = input.CalculationFormula ?? "",
            englishName = englishName,
            englishDescription = englishDescription
        };

        var data = await SendQueryAsync(query, variables);
        var resultJson = data.GetProperty("updateServiceSku").GetRawText();
        var dto = JsonSerializer.Deserialize<ServiceSkuDto>(resultJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        return dto;
    }

    public async Task<bool> DeleteServiceSkuAsync(Guid id)
    {
        var query = @"
            mutation($id: UUID!) {
                deleteServiceSku(id: $id)
            }";

        var variables = new { id = id };
        var data = await SendQueryAsync(query, variables);
        return data.GetProperty("deleteServiceSku").GetBoolean();
    }

    public async Task<List<ServiceSkuDto>> GetServiceSkusByCategoryAsync(Guid categoryId)
    {
        var query = @"
            query($categoryId: UUID!) {
                serviceSkusByCategory(categoryId: $categoryId) {
                    id
                    skuCode
                    name
                    description
                    basePrice
                    unitType
                    serviceCategoryId
                    calculationFormula
                    englishName
                    englishDescription
                }
            }";

        var variables = new { categoryId = categoryId };
        var data = await SendQueryAsync(query, variables);
        var skusJson = data.GetProperty("serviceSkusByCategory").GetRawText();
        var list = JsonSerializer.Deserialize<List<ServiceSkuDto>>(skusJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        return list;
    }

    public async Task<bool> DeleteServiceCategoryAsync(Guid id)
    {
        var query = @"
            mutation($id: UUID!) {
                deleteServiceCategory(id: $id)
            }";

        var variables = new { id = id };
        var data = await SendQueryAsync(query, variables);
        return data.GetProperty("deleteServiceCategory").GetBoolean();
    }

    public async Task<ImportResultDto> ImportSpiderNetConfigAsync(string json)
    {
        var query = @"
            mutation($json: String!) {
                importSpiderNetConfig(json: $json) {
                    success
                    logLines
                    errorMessage
                }
            }";

        var variables = new { json = json };
        var data = await SendQueryAsync(query, variables);
        var resultJson = data.GetProperty("importSpiderNetConfig").GetRawText();
        return JsonSerializer.Deserialize<ImportResultDto>(resultJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    public async Task<Dictionary<string, string>> GetEnglishTranslationsAsync()
    {
        var query = @"
            query {
                allLocalizationResources {
                    key
                    culture
                    value
                }
            }";

        var data = await SendQueryAsync(query);
        var resourcesJson = data.GetProperty("allLocalizationResources").GetRawText();
        var list = JsonSerializer.Deserialize<List<LocalizationResourceDto>>(resourcesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        return list
            .Where(r => string.Equals(r.Culture, "en", StringComparison.OrdinalIgnoreCase))
            .GroupBy(r => r.Key)
            .ToDictionary(g => g.Key, g => g.First().Value, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> UpdateTranslationAsync(string key, string culture, string value)
    {
        var query = @"
            mutation($input: UpdateLocalizationInput!) {
                updateLocalizationString(input: $input) {
                    id
                }
            }";

        var variables = new
        {
            input = new
            {
                key = key,
                culture = culture,
                value = value
            }
        };

        var data = await SendQueryAsync(query, variables);
        return data.GetProperty("updateLocalizationString").TryGetProperty("id", out _);
    }
}

public class LocalizationResourceDto
{
    public string Key { get; set; } = string.Empty;
    public string Culture { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
