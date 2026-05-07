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

	public async Task<AiTaskPricingResponse> CalculateTaskPricesAsync(List<JobTask> tasks, List<ServiceSku> allowedSkus, string languageCode = "en")
	{
		try
		{
			var prompt = new StringBuilder();
			prompt.AppendLine("SYSTEM PROMPT: TASK PRICING & SKU MAPPING");
			prompt.AppendLine("Role: You are an expert Construction Estimator. Your job is to strictly map homeowner tasks to the Allowed SKUs and determine appropriate quantities.");
			prompt.AppendLine();
			prompt.AppendLine("Output Format (JSON ONLY):");
			prompt.AppendLine("You MUST return ONLY a strict JSON object with the following structure:");
			prompt.AppendLine("{");
			prompt.AppendLine("  \"tasks\": [");
			prompt.AppendLine("    {");
			prompt.AppendLine("      \"taskId\": \"(Exact TaskId GUID from Input)\",");
			prompt.AppendLine("      \"taskTitle\": \"(Exact Task Title from Input)\",");
			prompt.AppendLine("      \"skuItems\": [");
			prompt.AppendLine("         { \"skuCode\": \"SKU_OVEN_LABOR\", \"quantity\": 1 },");
			prompt.AppendLine("         { \"skuCode\": \"SKU_DISPOSAL\", \"quantity\": 1 }");
			prompt.AppendLine("      ]");
			prompt.AppendLine("    }");
			prompt.AppendLine("  ]");
			prompt.AppendLine("}");
			prompt.AppendLine();
			prompt.AppendLine("ANTI-HALLUCINATION RULES:");
			prompt.AppendLine("1. SKU MAPPING: Only use SkuCodes from the Allowed SKUs list below. DO NOT HALLUCINATE SKUs.");
			prompt.AppendLine("2. NO EXTRA TASKS: Only return items for the tasks explicitly provided in the USER TASKS list.");
			prompt.AppendLine("3. MATCH EXACTLY: The taskId MUST match the provided task exactly so the system can match them back together.");
			prompt.AppendLine($"4. MULTILINGUAL SUPPORT: The homeowner tasks may be written in a different language. You must conceptually translate them and map them to the English Allowed SKUs, but YOU MUST RESPOND WITH taskTitle IN THE LANGUAGE DESIGNATED BY CODE '{languageCode.ToUpper()}'.");
			prompt.AppendLine();
			prompt.AppendLine("---");
			prompt.AppendLine("USER TASKS:");
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
			prompt.AppendLine("ALLOWED SKUS:");
			foreach (var sku in allowedSkus)
			{
				prompt.AppendLine($"- {sku.SkuCode}: {sku.Name} (Unit: {sku.UnitType}) - {sku.Description}");
			}
			prompt.AppendLine();
			prompt.AppendLine("Output ONLY valid JSON. Do not use Markdown blocks like ```json.");
			prompt.AppendLine("CRITICAL: Ensure all strings are properly escaped, all properties are quoted, and the JSON is strictly valid.");

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