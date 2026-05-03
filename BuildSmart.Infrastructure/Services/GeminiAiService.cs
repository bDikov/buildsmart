using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Application.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Json;

namespace BuildSmart.Infrastructure.Services;

public class GeminiAiService : IAiService
{
    private readonly string _apiKey;
    private readonly ILogger<GeminiAiService> _logger;
    private readonly HttpClient _httpClient;

    public GeminiAiService(IConfiguration configuration, ILogger<GeminiAiService> logger)
    {
        _apiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini:ApiKey is not configured.");
        _logger = logger;
        _httpClient = new HttpClient();
    }

    public async Task<AiScopeBreakdownResponse> GenerateJobScopeAsync(JobPost jobPost, string humanReadableContext, List<ServiceSku> allowedSkus, string language = "Bulgarian (Български език)")
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
            prompt.AppendLine("1. STRICT LIMIT: Only address the work explicitly mentioned in the User Answers. Do not add additional rooms, areas, or unrelated services.");
            prompt.AppendLine("2. TECHNICAL INFERENCE ONLY: Only infer sub-tasks strictly required to execute the requested work (e.g., if 'tile installation' is requested, infer 'grouting' and 'adhesive application', but do NOT infer 'underfloor heating' unless explicitly stated).");
            prompt.AppendLine("3. NO ASSUMPTIONS: If an answer is missing or ambiguous, do not guess. Instead, add a note: 'Contractor to verify [Specific Detail] on site'.");
            prompt.AppendLine("4. FACTUAL CONSISTENCY: Ensure every task in the SOW can be traced back to a specific user answer or a necessary technical dependency of that answer.");
            prompt.AppendLine("5. NO PRICE CALCULATION: Do not calculate prices or include prices in your response.");
            prompt.AppendLine($"6. LANGUAGE: ALL OUTPUT MUST BE IN {language.ToUpper()}. This is a strict requirement. The scopeMarkdown, taskTitle, taskDescription, and ALL items in acceptanceCriteria MUST be written in {language}, regardless of the input language.");
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

            var requestBody = new 
            {
                contents = new[] 
                {
                    new 
                    {
                        parts = new[] 
                        {
                            new { text = prompt.ToString() }
                        }
                    }
                },
                generationConfig = new 
                {
                    responseMimeType = "application/json"
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
            
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorString = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API failed with status {response.StatusCode}: {errorString}");
            }

            var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>();
            var responseText = responseJson.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? string.Empty;

            // Clean up possible markdown code blocks around json
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

            responseText = responseText.Trim();

            var result = JsonSerializer.Deserialize<AiScopeBreakdownResponse>(responseText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (result == null)
            {
                 throw new Exception("AI returned null or invalid JSON.");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating job scope with Gemini for Job {JobId}", jobPost.Id);
            throw; // Let the worker handle the failure
        }
    }

    public async Task<string> GenerateProjectSummaryAsync(Project project)
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
            prompt.AppendLine("5. Output ONLY the report.");

            var requestBody = new 
            {
                contents = new[] 
                {
                    new 
                    {
                        parts = new[] 
                        {
                            new { text = prompt.ToString() }
                        }
                    }
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
            
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                return "Failed to generate project summary.";
            }

            var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>();
            return responseJson.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "Failed to generate project summary.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating project summary with Gemini for Project {ProjectId}", project.Id);
            return "An error occurred while generating the project summary.";
        }
    }

    public async Task<AiTaskPricingResponse> CalculateTaskPricesAsync(List<JobTask> tasks, List<ServiceSku> allowedSkus)
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
            prompt.AppendLine("4. MULTILINGUAL SUPPORT: The homeowner tasks may be written in a different language (e.g. Bulgarian). You must conceptually translate them and map them to the English Allowed SKUs.");
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
            foreach(var sku in allowedSkus)
            {
                prompt.AppendLine($"- {sku.SkuCode}: {sku.Name} (Unit: {sku.UnitType}) - {sku.Description}");
            }
            prompt.AppendLine();
            prompt.AppendLine("Output ONLY valid JSON. Do not use Markdown blocks like ```json.");

            var requestBody = new 
            {
                contents = new[] 
                {
                    new 
                    {
                        parts = new[] 
                        {
                            new { text = prompt.ToString() }
                        }
                    }
                },
                generationConfig = new 
                {
                    responseMimeType = "application/json"
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
            
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorString = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini API failed with status {response.StatusCode}: {errorString}");
            }

            var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>();
            var responseText = responseJson.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? string.Empty;

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

            responseText = responseText.Trim();

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
