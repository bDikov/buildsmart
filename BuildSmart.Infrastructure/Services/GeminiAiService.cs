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
		var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			var errorString = await response.Content.ReadAsStringAsync(cancellationToken);
			throw new Exception($"Gemini API failed with status {response.StatusCode}: {errorString}");
		}

		var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
		return responseJson.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? string.Empty;
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
			return responseText;
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
			return responseText.Trim();
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
			prompt.AppendLine("Goal: Map 'USER TASKS' to the 'ALLOWED SKUS' and calculate the exact 'quantity' using the provided 'USER Q&A CONTEXT' and the 'CONSTRUCTION HEURISTICS' below.");
			prompt.AppendLine();
			prompt.AppendLine("### CONSTRUCTION HEURISTICS (QUANTITY CALCULATIONS):");
			prompt.AppendLine();
			prompt.AppendLine("1. --- GENERAL & OVERHEAD (GEN) ---");
			prompt.AppendLine("   - Site Protection (GEN-001): Quantity = 1 (Fixed). Map if task involves 'подготовка', 'защита', 'покриване'.");
			prompt.AppendLine("   - Final Waste Removal (GEN-002): Quantity = ID: global_total_sqm. Map if task involves 'почистване', 'извозване'.");
			prompt.AppendLine("   - Daily Logistics/Cleaning (GEN-003): Quantity = 1 (Fixed). Map if project duration/logistics mentioned.");
			prompt.AppendLine();
			prompt.AppendLine("2. --- ELECTRICAL (ELEC) ---");
			prompt.AppendLine("   - Cable Laying (ELEC-CABLE-LAY): If explicit meters unknown, use: (ID: global_total_sqm * 3.5).");
			prompt.AppendLine("   - Heavy Duty Cable (ELEC-CABLE-HEAVY): Use: (ID: elec_heavy_appliances + ID: elec_ac_count + ID: elec_boiler_count) * 10 meters.");
			prompt.AppendLine("   - Chasing (CONCRETE): If ID: elec_walls is 'Бетон' or 'Панел', use ELEC-CHASE-CONC. Quantity = (ID: global_total_sqm * 1.5).");
			prompt.AppendLine("   - Chasing (BRICK): If ID: elec_walls is 'Тухла', use ELEC-CHASE-BRICK. Quantity = (ID: global_total_sqm * 1.5).");
			prompt.AppendLine("   - Corrugated Tubes (ELEC-LAY-TUBE): Quantity = (ID: global_total_sqm * 2.5).");
			prompt.AppendLine("   - Standard Points (ELEC-POINT-STD): Quantity = ID: elec_outlets. (Includes outlets, switches, lamp points).");
			prompt.AppendLine("   - Deviator Points (ELEC-POINT-DEV): Quantity = ID: elec_deviators.");
			prompt.AppendLine("   - Special Points (ELEC-POINT-SPEC): Quantity = ID: elec_blinds_count + ID: elec_fans_count.");
			prompt.AppendLine("   - Low Voltage (ELEC-POINT-LV): Quantity = ID: elec_lan_tv_count + ID: elec_security_points.");
			prompt.AppendLine("   - Panel Modules (ELEC-PANEL-MOD): Quantity = 12 (base) + ID: elec_heavy_appliances + ID: elec_ac_count + (ID: global_bathroom_count * 2).");
			prompt.AppendLine("   - RCD Install (ELEC-RCD-INSTALL): Quantity = ID: global_bathroom_count + 1.");
			prompt.AppendLine();
			prompt.AppendLine("3. --- PAINTING (PANT) ---");
			prompt.AppendLine("   - Surface Calculation: If ID: paint_sqm is provided, use it. Otherwise, Wall & Ceiling Area = (ID: global_total_sqm * 2.5).");
			prompt.AppendLine("   - Priming (PANT-001): Quantity = Wall & Ceiling Area.");
			prompt.AppendLine("   - Painting (PANT-002/003/004): Quantity = Wall & Ceiling Area.");
			prompt.AppendLine("   - Q5 Finish (PANT-006): Quantity = Wall & Ceiling Area (ONLY if ID: paint_finish_level contains 'Q5').");
			prompt.AppendLine();
			prompt.AppendLine("4. --- PLUMBING (PLMB) ---");
			prompt.AppendLine("   - Water/Waste Points (PLMB-001/002): Quantity = ID: plumb_points. If unknown, use: (ID: global_bathroom_count * 5).");
			prompt.AppendLine("   - Fixture Install (PLMB-003): Quantity = ID: plumb_fixture_count.");
			prompt.AppendLine("   - Concealed Pipes (PLMB-007): Quantity = ID: plumb_concealed_meters.");
			prompt.AppendLine();
			prompt.AppendLine("### ANTI-HALLUCINATION & PRECISION RULES:");
			prompt.AppendLine("1. NO MADE-UP SKUS: Use ONLY SkuCodes from the provided 'ALLOWED SKUS' list. If no exact match, use the most logically related one.");
			prompt.AppendLine("2. GUID CONSISTENCY: You MUST return the EXACT TaskId GUID provided in the 'USER TASKS TO PRICE' list. Do not alter even one character.");
			prompt.AppendLine("3. NO '1' DEFAULTS: Never default to a quantity of 1 for items measured in 'm' or 'sqm' if project dimensions are available in the context.");
			prompt.AppendLine("4. LANGUAGE NEUTRAL MAPPING: The USER Q&A and TASKS are likely in Bulgarian. The SKUs are likely in English/Codes. Translate conceptually. (e.g., 'Контакт' -> 'ELEC-POINT-STD', 'Шпакловка' -> 'PANT').");
			prompt.AppendLine($"5. OUTPUT LANGUAGE: Output the `taskTitle` in the language of code '{languageCode.ToUpper()}'.");
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
}