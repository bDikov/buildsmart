using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Application.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace BuildSmart.Infrastructure.Services;

public class GeminiAiService : IAiService
{
	private readonly string _apiKey;
	private readonly string _model;
	private readonly ILogger<GeminiAiService> _logger;
	private readonly HttpClient _httpClient;

	public GeminiAiService(IConfiguration configuration, ILogger<GeminiAiService> logger)
	{
		var geminiKey = configuration["Gemini:ApiKey"];

		_apiKey = geminiKey ?? throw new ArgumentNullException("Gemini:ApiKey is not configured.");
		_model = "gemini-2.5-flash"; // Updated to current 2026 model

		_logger = logger;
		_httpClient = new HttpClient();
		_httpClient.Timeout = TimeSpan.FromHours(1); // Increase timeout for long AI generation
	}

	private async Task<string> ExecuteAiPromptAsync(string prompt, bool useJsonMode = false, CancellationToken cancellationToken = default)
	{
		object requestBody;
		if (useJsonMode)
		{
			requestBody = new
			{
				contents = new[]
				{
					new
					{
						parts = new[]
						{
							new { text = prompt }
						}
					}
				},
				generationConfig = new
				{
					responseMimeType = "application/json"
				}
			};
		}
		else
		{
			requestBody = new
			{
				contents = new[]
				{
					new
					{
						parts = new[]
						{
							new { text = prompt }
						}
					}
				}
			};
		}

		var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
		
		int maxRetries = 5;
		int delayMs = 30000;

		for (int i = 0; i < maxRetries; i++)
		{
			var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

			if (response.IsSuccessStatusCode)
			{
				var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
				return responseJson.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? string.Empty;
			}
			
			if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || (int)response.StatusCode == 429)
			{
				if (i == maxRetries - 1)
				{
					var errorString = await response.Content.ReadAsStringAsync(cancellationToken);
					throw new Exception($"Gemini API failed with status {response.StatusCode} after {maxRetries} retries: {errorString}");
				}
				
				_logger.LogWarning($"Gemini API returned {response.StatusCode}. Retrying in {delayMs}ms...");
				await Task.Delay(delayMs, cancellationToken);
				continue;
			}

			var fatalErrorString = await response.Content.ReadAsStringAsync(cancellationToken);
			throw new Exception($"Gemini API failed with status {response.StatusCode}: {fatalErrorString}");
		}

		throw new Exception("Gemini API failed unexpectedly.");
	}

	private string CleanJsonMarkdown(string responseText)
	{
		responseText = responseText.Trim();
		if (responseText.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
		{
			responseText = responseText.Substring(7);
			if (responseText.EndsWith("```"))
			{
				responseText = responseText.Substring(0, responseText.Length - 3);
			}
		}
		else if (responseText.StartsWith("```", StringComparison.OrdinalIgnoreCase))
		{
			responseText = responseText.Substring(3);
			if (responseText.EndsWith("```"))
			{
				responseText = responseText.Substring(0, responseText.Length - 3);
			}
		}

		return responseText.Trim();
	}

	public async Task<AiScopeBreakdownResponse> GenerateJobScopeAsync(JobPost jobPost, string humanReadableContext, List<ServiceSku> allowedSkus, string languageCode = "bg", CancellationToken cancellationToken = default)
	{
		try
		{
			var prompt = new StringBuilder();
			prompt.AppendLine("SYSTEM PROMPT: SMART SCOPE GENERATION");
			prompt.AppendLine("Role: You are an expert Construction Manager and Quantity Surveyor with 20+ years of experience. Your job is to draft professional, legally sound, and detailed Scopes of Work (SOW) based on raw homeowner inputs.");
			prompt.AppendLine();
			prompt.AppendLine("Goal: Transform simple answers into a comprehensive, professional document that a Tradesman can use to provide an accurate bid without needing to ask basic questions. You must also split the work into specific, manageable tasks.");
			prompt.AppendLine();
			prompt.AppendLine("Output Format (JSON ONLY):");
			prompt.AppendLine("You MUST return ONLY a strict JSON object with the following structure:");
			prompt.AppendLine("{");
			prompt.AppendLine("  \"scopeMarkdown\": \"(Generate a real, detailed, multi-paragraph Markdown document here based on the user's answers. Do NOT just copy this placeholder text.)\",");
			prompt.AppendLine("  \"tasks\": [");
			prompt.AppendLine("    {");
			prompt.AppendLine("      \"taskTitle\": \"Install Oven\",");
			prompt.AppendLine("      \"taskDescription\": \"Disconnect and remove old oven. Install new oven, connect electrical, and test.\",");
			prompt.AppendLine("      \"acceptanceCriteria\": [\"Oven is fully operational\", \"No electrical faults\", \"Site left clean\"]");
			prompt.AppendLine("    }");
			prompt.AppendLine("  ]");
			prompt.AppendLine("}");
			prompt.AppendLine();
			prompt.AppendLine("Tone Guidelines for ScopeMarkdown:");
			prompt.AppendLine("- Professional & Technical (e.g., 'Demo' instead of 'Break down').");
			prompt.AppendLine("- Objective: Factual language, no sales fluff.");
			prompt.AppendLine("- Defensive: Include standard clauses about 'compliance with local building codes' and 'obtaining necessary permits'.");
			prompt.AppendLine();
			prompt.AppendLine("ANTI-HALLUCINATION & SCOPE BOUNDARY RULES:");
			prompt.AppendLine($"0. STRICT CATEGORY ISOLATION: You are generating a scope ONLY for the '{(jobPost.ServiceCategory != null ? jobPost.ServiceCategory.Name : "General")}' category. Ignore any user input that belongs to other trades (e.g., if this is Electrical, DO NOT include Plumbing or Drywall tasks). THIS IS CRITICAL TO PREVENT DUPLICATION ACROSS THE PROJECT.");
			prompt.AppendLine("1. STRICT LIMIT: Only address the work explicitly mentioned in the User Answers that relates to YOUR assigned category. Do not add additional rooms, areas, or unrelated services.");
			prompt.AppendLine("2. TECHNICAL INFERENCE ONLY: Only infer sub-tasks strictly required to execute the requested work within your category.");
			prompt.AppendLine("3. NO ASSUMPTIONS: If an answer is missing or ambiguous, do not guess. Instead, add a note: 'Contractor to verify [Specific Detail] on site'.");
			prompt.AppendLine("4. FACTUAL CONSISTENCY: Ensure every task in the SOW can be traced back to a specific user answer or a necessary technical dependency of that answer.");
			prompt.AppendLine("5. NO PRICE CALCULATION: Do not calculate prices or include prices in your response.");
			prompt.AppendLine($"6. LANGUAGE ENFORCEMENT: ALL OUTPUT (scopeMarkdown, taskTitle, taskDescription, and acceptanceCriteria) MUST be strictly written in the language corresponding to the ISO code '{languageCode.ToUpper()}'. If the provided Q&A context or input is in a different language, you MUST translate it and generate your response entirely in '{languageCode.ToUpper()}'.");
			prompt.AppendLine("7. NO GENERIC OVERHEAD TASKS: Do NOT create separate tasks for 'Site Preparation', 'Logistics', 'Daily Cleaning', 'Material Delivery', or 'Final Waste Removal'. These are overhead. Include them as 'acceptanceCriteria' within the actual technical tasks.");
			prompt.AppendLine("8. NO MICRO-TASKING: Do not split standard services into micro-tasks. For example, 'Metal frame construction' and 'Boarding' should be a single task: 'Build Drywall'. 'Grouting/Spackling' is either included in Drywall or belongs to Painting. Consolidate technical steps into billable units.");
			prompt.AppendLine("9. CONSISTENT FORMATTING: ALWAYS use standard decimal digits (1., 2., 3.) for numbering sections and lists in your scopeMarkdown. NEVER use Roman numerals (I., II., III.).");
			prompt.AppendLine();
			prompt.AppendLine("---");
			prompt.AppendLine("USER INPUT DATA:");
			prompt.AppendLine($"Title: {jobPost.Title}");
			prompt.AppendLine($"Category: {(jobPost.ServiceCategory != null ? jobPost.ServiceCategory.Name : "General")}");
			prompt.AppendLine($"Location: {jobPost.Location}");
			prompt.AppendLine("Q&A:");
			prompt.AppendLine(humanReadableContext);
			prompt.AppendLine();
			prompt.AppendLine("Output ONLY valid JSON. Do not use Markdown blocks like ```json.");
			prompt.AppendLine("CRITICAL: Ensure all strings are properly escaped, all properties are quoted, and the JSON is strictly valid.");

			var responseText = await ExecuteAiPromptAsync(prompt.ToString(), useJsonMode: true);
			responseText = CleanJsonMarkdown(responseText);
			responseText = EscapeRawControlCharacters(responseText);

			var result = JsonSerializer.Deserialize<AiScopeBreakdownResponse>(responseText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			if (result == null)
			{
				throw new Exception("AI returned null or invalid JSON.");
			}

			return result;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error generating job scope with AI for Job {JobId}", jobPost.Id);
			throw;
		}
	}

	public async Task<string> GenerateProjectSummaryAsync(Project project, string languageCode = "bg", CancellationToken cancellationToken = default)
	{
		try
		{
			var prompt = new StringBuilder();
			prompt.AppendLine("You are a Senior Construction Program Manager.");
			prompt.AppendLine($"Generate a strategic project roadmap for the project: '{project.Title}'.");
			prompt.AppendLine($"Description: {project.Description}");
			prompt.AppendLine();
			prompt.AppendLine("### List of Jobs in this Project:");
			foreach (var job in project.JobPosts)
			{
				prompt.AppendLine($"- {job.Title} ({job.ServiceCategory.Name})");
			}
			prompt.AppendLine();
			prompt.AppendLine("### Requirements:");
			prompt.AppendLine("1. Summarize the overall project goal.");
			prompt.AppendLine("2. Identify the optimal sequence of work (which job should happen first, second, etc.).");
			prompt.AppendLine("3. Highlight potential 'Trade Interferences' (e.g., plumbing must be finished before drywall).");
			prompt.AppendLine("4. Use professional Markdown formatting.");
			prompt.AppendLine($"5. Output ONLY the report IN THE LANGUAGE DESIGNATED BY CODE '{languageCode.ToUpper()}'.");

			var responseText = await ExecuteAiPromptAsync(prompt.ToString());
			return CleanLanguagePrefix(responseText);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error generating project summary with AI for Project {ProjectId}", project.Id);
			return "An error occurred while generating the project summary.";
		}
	}

	public async Task<string> GenerateExecutiveSummaryAsync(string combinedScopes, string languageCode = "bg", CancellationToken cancellationToken = default)
	{
		try
		{
			var prompt = new StringBuilder();
			prompt.AppendLine("You are a Senior Construction Estimator.");
			prompt.AppendLine("Below are the detailed scopes of work for several trades involved in a project.");
			prompt.AppendLine("Your goal is to write a short, professional, and well-descriptive 'Executive Summary' (around 2 to 3 paragraphs) that gives the client a high-level overview of everything that will be done across all trades. Do NOT list items bullet by bullet, but rather summarize the transformation and the main tasks.");
			prompt.AppendLine();
			prompt.AppendLine("### DETAILED SCOPES:");
			prompt.AppendLine(combinedScopes);
			prompt.AppendLine();
			prompt.AppendLine($"Output ONLY the executive summary IN THE LANGUAGE DESIGNATED BY CODE '{languageCode.ToUpper()}'.");

			var responseText = await ExecuteAiPromptAsync(prompt.ToString(), false, cancellationToken);
			return CleanLanguagePrefix(responseText);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error generating executive summary with AI");
			throw;
		}
	}

	public async Task<AiTaskPricingResponse> CalculateTaskPricesAsync(List<JobTask> tasks, List<ServiceSku> allowedSkus, string humanReadableContext, string languageCode = "bg", CancellationToken cancellationToken = default)
	{
		try
		{
			var prompt = new StringBuilder();
			prompt.AppendLine("SYSTEM PROMPT: EXPERT CONSTRUCTION ESTIMATOR & SKU MAPPER");
			prompt.AppendLine("Role: You are a Master Quantity Surveyor and Senior Construction Estimator with 25 years of experience in the European market. Your job is to translate qualitative homeowner answers into quantitative billable items (SKUs) with high precision.");
			prompt.AppendLine();
			prompt.AppendLine("Goal: Map 'USER TASKS' to the 'ALLOWED SKUS' using the provided 'USER Q&A CONTEXT'.");
			prompt.AppendLine();
			prompt.AppendLine("### INSTRUCTIONS:");
			prompt.AppendLine("1. Review the user's answers. If the requested work requires a specific SKU, output that SKU code.");
			prompt.AppendLine("2. DO NOT CALCULATE QUANTITIES. The C# Pricing Engine will calculate the exact quantities based on the user's answers.");
			prompt.AppendLine("3. Assume a quantity of 1 for every SKU you map to indicate that the SKU is required for the task.");
			prompt.AppendLine("4. NO MADE-UP SKUS: Use ONLY SkuCodes from the provided 'ALLOWED SKUS' list. If no exact match, use the most logically related one.");
			prompt.AppendLine("5. GUID CONSISTENCY: You MUST return the EXACT TaskId GUID provided in the 'USER TASKS TO PRICE' list. Do not alter even one character.");
			prompt.AppendLine("6. LANGUAGE NEUTRAL MAPPING: The USER Q&A and TASKS are likely in Bulgarian. The SKUs are likely in English/Codes. Translate conceptually.");
			prompt.AppendLine("7. LABOR IS ALWAYS BILLABLE: Even if a user states they will 'provide/buy the materials' (e.g., sinks, toilets, tiles), you MUST STILL map the Labor Installation SKUs (e.g., PLMB-SINK-INSTALL, PLMB-WC-STD). Tradesmen always charge for labor.");
			prompt.AppendLine("8. IGNORE EXTERNAL TRADES: If a task says 'Demolition' but you don't have a 'DEMO' SKU in the ALLOWED SKUS list, leave it empty. Do not guess.");
			prompt.AppendLine($"9. OUTPUT LANGUAGE: Output the `taskTitle` in the language of code '{languageCode.ToUpper()}'.");
			prompt.AppendLine();
			prompt.AppendLine("### OUTPUT FORMAT (STRICT JSON ONLY):");
			prompt.AppendLine("{");
			prompt.AppendLine("  \"tasks\": [");
			prompt.AppendLine("    {");
			prompt.AppendLine("      \"taskId\": \"(GUID)\",");
			prompt.AppendLine("      \"taskTitle\": \"(Title in designated language)\",");
			prompt.AppendLine("      \"skuItems\": [");
			prompt.AppendLine("         { \"skuCode\": \"SKU-001\", \"quantity\": 45.5 }");
			prompt.AppendLine("      ]");
			prompt.AppendLine("    }");
			prompt.AppendLine("  ]");
			prompt.AppendLine("}");
			prompt.AppendLine();
			prompt.AppendLine("---");
			prompt.AppendLine("USER Q&A CONTEXT:");
			prompt.AppendLine(humanReadableContext);
			prompt.AppendLine();
			prompt.AppendLine("---");
			prompt.AppendLine("USER TASKS TO PRICE:");
			foreach (var task in tasks)
			{
				prompt.AppendLine($"- ID: {task.Id}");
				prompt.AppendLine($"  Title: {task.Title}");
				prompt.AppendLine($"  Description: {task.Description}");
			}
			prompt.AppendLine();
			prompt.AppendLine("---");
			prompt.AppendLine("ALLOWED SKUS:");
			foreach (var sku in allowedSkus)
			{
				prompt.AppendLine($"- {sku.SkuCode}: {sku.Name} (Unit: {sku.UnitType}) - {sku.Description}");
			}
			prompt.AppendLine();
			prompt.AppendLine("Output ONLY valid JSON. No markdown formatting.");

			_logger.LogInformation("Estimator Expert: Processing pricing for {TaskCount} tasks.", tasks.Count);

			var responseText = await ExecuteAiPromptAsync(prompt.ToString(), useJsonMode: true);
			var rawResponse = responseText;
			responseText = CleanJsonMarkdown(responseText);
			responseText = EscapeRawControlCharacters(responseText);

			try
			{
				var result = JsonSerializer.Deserialize<AiTaskPricingResponse>(responseText, new JsonSerializerOptions 
				{ 
					PropertyNameCaseInsensitive = true,
					NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
				});

				if (result == null || result.Tasks == null)
				{
					_logger.LogError("Estimator Expert Error: AI returned empty result. Raw response: {RawResponse}", rawResponse);
					throw new Exception("AI returned null or invalid JSON.");
				}

				// Validation and Sentry Breadcrumbs
				foreach (var taskItem in result.Tasks)
				{
					if (!Guid.TryParse(taskItem.TaskId, out var taskGuid) || !tasks.Any(t => t.Id == taskGuid))
					{
						_logger.LogWarning("Estimator Expert Warning: AI returned unknown TaskId {UnknownId}.", taskItem.TaskId);
						continue;
					}
					
					if (taskItem.SkuItems == null || !taskItem.SkuItems.Any())
					{
						_logger.LogWarning("Estimator Expert Warning: Task {TaskId} has NO mapped SKUs.", taskItem.TaskId);
					}

					foreach (var skuItem in taskItem.SkuItems)
					{
						if (!allowedSkus.Any(s => s.SkuCode == skuItem.SkuCode))
						{
							_logger.LogWarning("Estimator Expert Warning: Hallucinated SkuCode {UnknownSku} for Task {TaskId}.", skuItem.SkuCode, taskItem.TaskId);
						}
					}
				}

				return result;
			}
			catch (JsonException jex)
			{
				_logger.LogError(jex, "Estimator Expert Error: JSON Deserialization failed. Raw: {RawResponse}", rawResponse);
				throw;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Estimator Expert Fatal: Error pricing tasks with Gemini");
			throw;
		}
	}

	private string EscapeRawControlCharacters(string json)
	{
		if (string.IsNullOrEmpty(json)) return json;

		var sb = new StringBuilder();
		bool inString = false;
		bool isEscaped = false;

		for (int i = 0; i < json.Length; i++)
		{
			char c = json[i];

			if (inString)
			{
				if (isEscaped)
				{
					sb.Append(c);
					isEscaped = false;
				}
				else if (c == '\\')
				{
					sb.Append(c);
					isEscaped = true;
				}
				else if (c == '"')
				{
					sb.Append(c);
					inString = false;
				}
				else if (c == '\n')
				{
					sb.Append("\\n");
				}
				else if (c == '\r')
				{
					sb.Append("\\r");
				}
				else if (c == '\t')
				{
					sb.Append("\\t");
				}
				else if (char.IsControl(c))
				{
					sb.AppendFormat("\\u{0:x4}", (int)c);
				}
				else
				{
					sb.Append(c);
				}
			}
			else
			{
				if (c == '"')
				{
					inString = true;
				}
				sb.Append(c);
			}
		}

		return sb.ToString();
	}

	private string CleanLanguagePrefix(string text)
	{
		if (string.IsNullOrWhiteSpace(text)) return text;
		var trimmed = text.Trim();
		var match = System.Text.RegularExpressions.Regex.Match(trimmed, @"^(EN|BG)\s*(:|-|\r?\n)\s*", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
		if (match.Success)
		{
			trimmed = trimmed.Substring(match.Length).Trim();
		}
		return trimmed;
	}

	public async Task<string> GenerateChatReplyAsync(string projectContext, string userMessage, string languageCode = "bg", CancellationToken cancellationToken = default)
	{
		var isBg = languageCode.Equals("bg", StringComparison.OrdinalIgnoreCase);
		var prompt = $@"Ти си опитен строителен консултант и интелигентен AI асистент за водещата строителна платформа BuildSmart.
Твоята цел е да водиш естествен, полезен и логичен разговор с клиента (отговори в рамките на 2-4 изречения).
Отговори на същия език като на клиента (по подразбиране: {(isBg ? "Български" : "English")}).

КРИТИЧНИ ПРАВИЛА ЗА ПОВЕДЕНИЕ В ЧАТА (ПАМЕТ И КОНТЕКСТ):
1. ПАЗИ ПЪЛНА ПАМЕТ: Внимателно прочети историята на чата по-долу. Помни всичко, което клиентът вече ти е споделил (размери, етаж, квадратура, материали, специфични желания).
2. НИКОГА НЕ ПОВТАРЯЙ ВЪПРОСИ: Категорично е забранено да питаш за детайли, на които клиентът вече е отговорил в предишните съобщения!
3. НЕ СЕ ПРЕДСТАВЯЙ ПОВТОРНО: Ако в историята на чата вече има поздрав или представяне, НЕ казвай отново 'Здравейте! Аз съм Вашият консултант...' във всяко следващо съобщение. Продължи разговора директно и естествено.
4. СВЪРЗВАЙ КРАТКИТЕ РЕПЛИКИ: Ако клиентът отговори само с една дума или цифра (напр. '3-ти етаж', 'Да', 'Около 50 кв.м', 'Имаме плочки'), разбери репликата му в контекста на твоя предишен въпрос и премини напред.
5. БЕЗ ДОСАДНИ ВЪПРОСИ: Не е задължително да задаваш въпрос при всяко съобщение. Ако клиентът просто търси съвет или информация, отговори му изчерпателно. Задавай въпрос само когато наистина ти липсва ключов строителен детайл.
6. БЕЗ ЕМОДЖИТА: Не използвай никакви емоджита (забранени са от стандартите на BuildSmart).
7. ОРИЕНТИРОВЪЧНИ ЦЕНИ И ДИАПАЗОНИ (LANDING PAGE ESTIMATOR):
   Когато клиентът пита за цена, бюджет, стойност на квадрат или 'колко ще ми струва', НЕ отказвай отговор и НЕ казвай просто 'не знам, трябва оглед'!
   Вместо това използвай официалните тарифи от BuildSmart калкулатора за ремонти (https://buildsmart.bg/renovation-estimator) и дай реалистичен ориентировъчен ценови диапазон:
   - Цялостен ремонт (труд + черни материали):
     * Довършителен / ново строителство (шпакловка и замазка): €370 – €440 / кв.м (~724 – 860 лв./кв.м).
     * Ремонт 'на тапа' (БДС груб строеж): €400 – €470 / кв.м (~782 – 919 лв./кв.м).
     * Старо строителство (панел/тухла с пълно къртене и извозване): €450 – €540 / кв.м (~880 – 1,056 лв./кв.м).
     * Мебели по поръчка (кухня, гардероби до таван): добавя се €230 – €350 / кв.м.
   - Ремонт на баня (~4 кв.м до ключ):
     * Труд и черни материали (ВиК, хидроизолация, плочки): €1,700 – €2,600.
     * Санитарно оборудване (структура, сифон, фаянс, смесители): €1,800 – €2,600.
     * Общо завършена баня: €3,500 – €5,200 (ново строителство) или €4,000 – €5,800 (старо строителство с къртене).
   - Ако клиентът е посочил квадратура (напр. 65 кв.м), изчисли ориентировъчната обща сума в евро и лева според типа строителство.
   - ВИНАГИ подчертавай фирмените предимства на BuildSmart: 0% аванс за труд (плащания на етапи с двустранни приемо-предавателни протоколи) и фиксирана цена по договор (КСС), която се финализира след оглед на място.
   - Препоръчай му да изчисли подробна оферта по конкретни СМР пера в нашия калкулатор: https://buildsmart.bg/renovation-estimator.
8. ПРОАКТИВНО ВЗЕМАНЕ НА КОНТАКТ ЗА ОГЛЕД И ОФЕРТА (LEAD CAPTURE):
   - Когато клиентът прояви интерес към ремонт, попита за конкретна цена, оферта, срокове или оглед, учтиво му предложи безплатен оглед на място от наш технически ръководител за изготвяне на точна оферта (КСС) с 0% аванс.
   - Попитай го учтиво за телефон за връзка и удобно време, за да се свърже наш екип с него.
   - Когато клиентът предостави телефон за връзка или контакт, БЛАГОДАРИ му учтиво и го увери, че неговият контакт е приет успешно и наш технически ръководител ще се свърже с него в най-кратък срок за организиране на безплатен оглед.

Контекст на проекта и предишна история на чата:
{projectContext}

Последно съобщение от клиента:
""{userMessage}""

Твоят отговор:";

		try
		{
			var reply = await ExecuteAiPromptAsync(prompt, useJsonMode: false, cancellationToken);
			return CleanLanguagePrefix(reply).Trim();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "[GeminiAiService] Failed to generate AI chat reply. Falling back to default message.");
			return isBg
				? "Здравейте! Благодарим за съобщението. Преглеждаме детайлите по Вашия проект и наш представител ще Ви отговори скоро."
				: "Hello! Thank you for your message. We are reviewing your project details and a representative will reply shortly.";
		}
	}

	public async Task<string> GenerateLeadSummaryAsync(Project project, CancellationToken cancellationToken = default)
	{
		var jobsInfo = new StringBuilder();
		if (project.JobPosts != null)
		{
			foreach (var job in project.JobPosts)
			{
				jobsInfo.AppendLine($"- Дейност: {job.Title} (Категория: {job.ServiceCategory?.Name ?? "Обща"})");
				if (!string.IsNullOrWhiteSpace(job.JobDetails) && job.JobDetails != "{}")
				{
					jobsInfo.AppendLine($"  Детайли/Отговори: {job.JobDetails}");
				}
				if (!string.IsNullOrWhiteSpace(job.Description))
				{
					jobsInfo.AppendLine($"  Описание: {job.Description}");
				}
			}
		}

		var prompt = $@"Направи кратък, ясен и структуриран анализ на български език (до 3-4 изречения или точки) за нов лийд за строително-ремонтен проект.
Посочи:
1. Ключов обхват на дейностите
2. Специфики или забележки от отговорите на клиента (ако има квадратури, специфични материали, етаж)
3. Потенциални въпроси за изясняване от екипа
НЕ използвай емоджита.

Проект: {project.Title}
Описание: {project.Description}
Дейности и детайли:
{jobsInfo}

Анализ:";

		try
		{
			var summary = await ExecuteAiPromptAsync(prompt, useJsonMode: false, cancellationToken);
			return CleanLanguagePrefix(summary).Trim();
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "[GeminiAiService] Failed to generate AI lead summary.");
			return $"Проект: {project.Title}. Включва {project.JobPosts?.Count ?? 0} дейности.";
		}
	}
}