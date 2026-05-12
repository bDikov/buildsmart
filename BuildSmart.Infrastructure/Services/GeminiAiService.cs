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
		_model = "gemini-2.5-flash"; // Default direct Gemini model

		_logger = logger;
		_httpClient = new HttpClient();
		_httpClient.Timeout = TimeSpan.FromHours(1); // Increase timeout for long AI generation
	}

	private async Task<string> ExecuteAiPromptAsync(string prompt, bool useJsonMode = false)
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
		var response = await _httpClient.PostAsJsonAsync(url, requestBody);

		if (!response.IsSuccessStatusCode)
		{
			var errorString = await response.Content.ReadAsStringAsync();
			throw new Exception($"Gemini API failed with status {response.StatusCode}: {errorString}");
		}

		var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>();
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

	public async Task<AiScopeBreakdownResponse> GenerateJobScopeAsync(JobPost jobPost, string humanReadableContext, List<ServiceSku> allowedSkus, string languageCode = "en")
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
			prompt.AppendLine($"6. LANGUAGE: ALL OUTPUT MUST BE IN THE LANGUAGE DESIGNATED BY CODE '{languageCode.ToUpper()}'. This is a strict requirement. The scopeMarkdown, taskTitle, taskDescription, and ALL items in acceptanceCriteria MUST be written in {languageCode}, regardless of the input language.");
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

	public async Task<string> GenerateProjectSummaryAsync(Project project, string languageCode = "en")
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

	public async Task<string> GenerateExecutiveSummaryAsync(string combinedScopes, string languageCode = "en")
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

			var responseText = await ExecuteAiPromptAsync(prompt.ToString());
			return responseText.Trim();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error generating executive summary with AI");
			throw;
		}
	}

	public async Task<AiTaskPricingResponse> CalculateTaskPricesAsync(List<JobTask> tasks, List<ServiceSku> allowedSkus, string humanReadableContext, string languageCode = "en")
	{
		try
		{
			var prompt = new StringBuilder();
			prompt.AppendLine("SYSTEM PROMPT: TASK PRICING & SKU MAPPING");
			prompt.AppendLine("Role: You are an expert Construction Estimator and Quantity Surveyor. Your job is to strictly map homeowner tasks to the Allowed SKUs and determine precise quantities based on the project dimensions.");
			prompt.AppendLine();
			prompt.AppendLine("Goal: Analyze the 'USER TASKS' and look for matching data in the 'USER Q&A CONTEXT' (sqm, linear meters, counts, etc.) to determine the correct 'quantity' for each SKU. DO NOT default to a quantity of 1 if the Q&A context provides dimensions.");
			prompt.AppendLine("CRITICAL RULE: You MUST output the EXACT SkuCode provided in the Allowed SKUs list. Do not make up SKUs. If the allowed SKU is named UNK-004, output UNK-004.");
			prompt.AppendLine();
			prompt.AppendLine("CONSTRUCTION HEURISTICS (USE THESE TO CALCULATE QUANTITIES):");
			prompt.AppendLine();
			prompt.AppendLine("--- GENERAL OVERHEAD (GEN) ---");
			prompt.AppendLine("- Site Prep & Protection & Logistics (Any task describing 'Подготовка', 'Логистика', 'Защита'): Map to GEN-001 with Quantity = 1.");
			prompt.AppendLine("- Final Cleaning (Any task describing 'Окончателно почистване', 'Финално почистване'): Map to GEN-002 with Quantity = global_total_sqm.");
			prompt.AppendLine("- Daily Cleaning (Any task describing 'Ежедневно почистване'): Map to GEN-003 with Quantity = 1.");
			prompt.AppendLine();
			prompt.AppendLine("--- ELECTRICAL (ELEC) ---");
			prompt.AppendLine("- Cable lengths (Any SKU describing 'Полагане на кабел' / 'Cable Laying'): If explicit meters are unknown, calculate Quantity = global_total_sqm * 4.");
			prompt.AppendLine("- Heavy Cable/Appliance Points: Quantity = 1 per heavy appliance/boiler (or 10 meters of heavy cable per appliance).");
			prompt.AppendLine("- Chasing/Channeling (Any SKU describing 'Къртене на канал' / 'Chasing'): Calculate Quantity = global_total_sqm * 1.5.");
			prompt.AppendLine("- Tubes/Corrugated pipes: Calculate Quantity = global_total_sqm * 2.5.");
			prompt.AppendLine("- Module Count (SKU for 'Подвързване на табло (на модул)'): Quantity = 12 (base) + 1 per heavy appliance + 1 per AC + 3 for RCDs.");
			prompt.AppendLine("- Outlets/Points (SKUs for 'Изграждане на излазна точка' or 'Монтаж на контакт'): Quantity = elec_outlets. If missing, Quantity = global_total_sqm / 5.");
			prompt.AppendLine("- Weak Current (LAN/TV/Security points): Quantity = elec_lan_tv_count + elec_security_points.");
			prompt.AppendLine("- Deviators: Quantity = elec_deviators.");
			prompt.AppendLine("- Special Points (Fans/Blinds): Quantity = elec_fans_count + elec_blinds_count.");
			prompt.AppendLine();
			prompt.AppendLine("--- PAINTING & DRYWALL (PANT / DRYW) ---");
			prompt.AppendLine("- Paint/Primer/Plaster Area: If paint_sqm is provided, use it. Otherwise, calculate walls and ceilings area as: global_total_sqm * 2.5.");
			prompt.AppendLine("- Painting Quantities (PANT-001, PANT-002, PANT-003, PANT-004): Quantity = Wall & Ceiling Area.");
			prompt.AppendLine("- Doors/Trim Painting (PANT-005): Quantity = paint_trim_doors_count.");
			prompt.AppendLine("- Q5 Plastering (PANT-006): Quantity = Wall & Ceiling Area (if paint_finish_level is Q5).");
			prompt.AppendLine("- Drywall Area (DRYW-001, DRYW-002, DRYW-003): If drywall_sqm is provided, use it. Otherwise, use global_total_sqm.");
			prompt.AppendLine("- Complex Shapes/Arches (DRYW-004): Quantity = drywall_complexity_lm.");
			prompt.AppendLine("- Pipe Boxes (DRYW-005): Quantity = drywall_boxes_lm.");
			prompt.AppendLine("- Wet Room Drywall (DRYW-006): Quantity = drywall_wet_room_sqm.");
			prompt.AppendLine();
			prompt.AppendLine("--- TILING & FLOORING (TILE) ---");
			prompt.AppendLine("- Tiling Area (TILE-001): Quantity = tile_sqm_standard.");
			prompt.AppendLine("- Large Format Tiling (TILE-003): Quantity = tile_sqm_large.");
			prompt.AppendLine("- Laminate Flooring (TILE-004): Quantity = tile_sqm_laminate.");
			prompt.AppendLine("- Stone Cladding (TILE-008): Quantity = tile_sqm_stone.");
			prompt.AppendLine("- Skirting/Zokal (TILE-002): Quantity (linear meters) = (tile_sqm_standard + tile_sqm_large + tile_sqm_laminate) / 1.5.");
			prompt.AppendLine("- Leveling Screed (TILE-009): Quantity = tile_leveling_sqm.");
			prompt.AppendLine("- Waterproofing (TILE-005): Quantity = tile_waterproofing_sqm.");
			prompt.AppendLine("- Epoxy Grout (TILE-006): Quantity = Area (sqm) (if tile_grout is Epoxy).");
			prompt.AppendLine("- Complex Pattern (TILE-007): Quantity = Area (sqm) (if tile_pattern is Complex).");
			prompt.AppendLine();
			prompt.AppendLine("--- MICROCEMENT (MICO) ---");
			prompt.AppendLine("- Microcement Area (MICO-001, MICO-002): Quantity = mico_sqm. Use MICO-002 for wet rooms/bathrooms, MICO-001 otherwise.");
			prompt.AppendLine("- Surface Prep (MICO-003): Quantity = mico_sqm (if mico_surface is 'Стари плочки' / 'Old Tiles').");
			prompt.AppendLine("- Waterproofing (MICO-004): Quantity = mico_waterproofing_sqm.");
			prompt.AppendLine();
			prompt.AppendLine("--- PLUMBING (PLMB) ---");
			prompt.AppendLine("- Pipes/Channels (PLMB-001, PLMB-002): Quantity = 1 per object/flat OR global_bathroom_count.");
			prompt.AppendLine("- Point Installs (PLMB-003): Quantity = plumb_fixture_count.");
			prompt.AppendLine("- Built-in Structures (PLMB-006): Quantity = plumb_builtin_count.");
			prompt.AppendLine("- Concealed Pipes (PLMB-007): Quantity = plumb_concealed_meters.");
			prompt.AppendLine("- Relocation/Chasing (PLMB-005): Quantity = plumb_relocated_points.");
			prompt.AppendLine();
			prompt.AppendLine("--- DEMOLITION (DEMO) ---");
			prompt.AppendLine("- Demolition Area/Volume (DEMO-001, DEMO-002): Quantity = demo_sqm or demo_cubic_m from Q&A.");
			prompt.AppendLine("- Rubble Disposal (DEMO-003): Quantity = demo_containers_count.");
			prompt.AppendLine("- Subfloor Demolition (DEMO-005): Quantity = demo_subfloor_sqm.");
			prompt.AppendLine("- Wallpaper Removal (DEMO-004): Quantity = paint_sqm (if paint_tasks includes wallpaper removal).");
			prompt.AppendLine();
			prompt.AppendLine("Output Format (JSON ONLY):");
			prompt.AppendLine("You MUST return ONLY a strict JSON object with the following structure:");
			prompt.AppendLine("{");
			prompt.AppendLine("  \"tasks\": [");
			prompt.AppendLine("    {");
			prompt.AppendLine("      \"taskId\": \"(Exact TaskId GUID from Input)\",");
			prompt.AppendLine("      \"taskTitle\": \"(Exact Task Title from Input)\",");
			prompt.AppendLine("      \"skuItems\": [");
			prompt.AppendLine("         { \"skuCode\": \"SKU_PAINTING_LABOR\", \"quantity\": 230 },");
			prompt.AppendLine("         { \"skuCode\": \"SKU_SANDING\", \"quantity\": 230 }");
			prompt.AppendLine("      ]");
			prompt.AppendLine("    }");
			prompt.AppendLine("  ]");
			prompt.AppendLine("}");
			prompt.AppendLine();
			prompt.AppendLine("CRITICAL QUANTITY RULES:");
			prompt.AppendLine("1. EXTRACT DIMENSIONS: If the USER Q&A CONTEXT mentions 'Area: 230 sqm' and the task is 'Painting', the quantity MUST be 230.");
			prompt.AppendLine("2. NO UNIT PRICES AS TOTALS: Never return a quantity of 1 for items that are clearly area-based or length-based if dimensions are available.");
			prompt.AppendLine("3. SKU MAPPING: Only use SkuCodes from the Allowed SKUs list below. DO NOT HALLUCINATE SKUs.");
			prompt.AppendLine("4. MATCH EXACTLY: The taskId MUST match the provided task exactly.");
			prompt.AppendLine($"5. LANGUAGE: Respond with taskTitle in the language designated by code '{languageCode.ToUpper()}'.");
			prompt.AppendLine();
			prompt.AppendLine("---");
			prompt.AppendLine("USER Q&A CONTEXT (Dimensions and Requirements):");
			prompt.AppendLine(humanReadableContext);
			prompt.AppendLine();
			prompt.AppendLine("---");
			prompt.AppendLine("USER TASKS TO PRICE:");
			foreach (var task in tasks)
			{
				prompt.AppendLine($"- ID: {task.Id}");
				prompt.AppendLine($"  Title: {task.Title}");
				prompt.AppendLine($"  Description: {task.Description}");
				if (task.AcceptanceCriteria != null && task.AcceptanceCriteria.Any())
				{
					prompt.AppendLine($"  Criteria: {string.Join(", ", task.AcceptanceCriteria.Select(c => c.Description))}");
				}
			}
			prompt.AppendLine();
			prompt.AppendLine("---");
			prompt.AppendLine("ALLOWED SKUS (Use these codes):");
			foreach (var sku in allowedSkus)
			{
				prompt.AppendLine($"- {sku.SkuCode}: {sku.Name} (Unit: {sku.UnitType}) - {sku.Description}");
			}
			prompt.AppendLine();
			prompt.AppendLine("Output ONLY valid JSON. Do not use Markdown blocks like ```json.");

			var responseText = await ExecuteAiPromptAsync(prompt.ToString(), useJsonMode: true);
			responseText = CleanJsonMarkdown(responseText);

			var result = JsonSerializer.Deserialize<AiTaskPricingResponse>(responseText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			if (result == null)
			{
				throw new Exception("AI returned null or invalid JSON.");
			}

			return result;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error pricing tasks with Gemini");
			throw; // Let Polly handle the retry in the worker!
		}
	}
}