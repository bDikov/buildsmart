using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mscc.GenerativeAI;
using System.Text;

namespace BuildSmart.Infrastructure.Services;

public class GeminiAiService : IAiService
{
    private readonly string _apiKey;
    private readonly ILogger<GeminiAiService> _logger;

    public GeminiAiService(IConfiguration configuration, ILogger<GeminiAiService> logger)
    {
        _apiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini:ApiKey is not configured.");
        _logger = logger;
    }

    public async Task<string> GenerateJobScopeAsync(JobPost jobPost)
    {
        try
        {
            var googleAi = new GoogleAI(_apiKey);
            var model = googleAi.GenerativeModel("gemini-1.5-pro");

            var prompt = new StringBuilder();
            prompt.AppendLine("SYSTEM PROMPT: SMART SCOPE GENERATION");
            prompt.AppendLine("Role: You are an expert Construction Manager, Quantity Surveyor, and Estimator with 20+ years of experience. Your job is to draft professional, legally sound, and detailed Scopes of Work (SOW) based on raw homeowner inputs.");
            prompt.AppendLine();
            prompt.AppendLine("Goal: Transform simple answers into a comprehensive, professional document that a Tradesman can use to provide an accurate bid without needing to ask basic questions.");
            prompt.AppendLine();
            prompt.AppendLine("Output Format (Markdown):");
            prompt.AppendLine("1. Project Overview: 2-3 sentence executive summary.");
            prompt.AppendLine("2. Detailed Scope of Work: Bulleted list of tasks. INFER necessary sub-tasks (e.g., if 'Replace Sink', include disconnection, removal, installation, and leak testing).");
            prompt.AppendLine("3. Materials & Fixtures: Separate 'Contractor Supplied' (rough-ins, adhesives) vs 'Owner Supplied' (finishes).");
            prompt.AppendLine("4. Site Conditions & Logistics: Address access, parking, noise, and cleanup.");
            prompt.AppendLine("5. Exclusions: Specify what is NOT included.");
            prompt.AppendLine();
            prompt.AppendLine("Tone Guidelines:");
            prompt.AppendLine("- Professional & Technical (e.g., 'Demo' instead of 'Break down').");
            prompt.AppendLine("- Objective: Factual language, no sales fluff.");
            prompt.AppendLine("- Defensive: Include standard clauses about 'compliance with local building codes' and 'obtaining necessary permits'.");
            prompt.AppendLine();
            prompt.AppendLine("ANTI-HALLUCINATION & SCOPE BOUNDARY RULES:");
            prompt.AppendLine("1. STRICT LIMIT: Only address the work explicitly mentioned in the User Answers. Do not add additional rooms, areas, or unrelated services.");
            prompt.AppendLine("2. TECHNICAL INFERENCE ONLY: Only infer sub-tasks strictly required to execute the requested work (e.g., if 'tile installation' is requested, infer 'grouting' and 'adhesive application', but do NOT infer 'underfloor heating' unless explicitly stated).");
            prompt.AppendLine("3. NO ASSUMPTIONS: If an answer is missing or ambiguous, do not guess. Instead, add a note: 'Contractor to verify [Specific Detail] on site'.");
            prompt.AppendLine("4. FACTUAL CONSISTENCY: Ensure every task in the SOW can be traced back to a specific user answer or a necessary technical dependency of that answer.");
            prompt.AppendLine();
            prompt.AppendLine("---");
            prompt.AppendLine("USER INPUT DATA:");
            prompt.AppendLine($"Title: {jobPost.Title}");
            prompt.AppendLine($"Category: {jobPost.ServiceCategory.Name}");
            prompt.AppendLine($"Location: {jobPost.Location}");
            prompt.AppendLine($"Answers: {jobPost.JobDetails}");
            prompt.AppendLine();
            prompt.AppendLine("Output ONLY the Markdown report.");

            var response = await model.GenerateContent(prompt.ToString());
            return response.Text ?? "Failed to generate scope content.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating job scope with Gemini for Job {JobId}", jobPost.Id);
            return "An error occurred while generating the scope. Please try again or contact support.";
        }
    }

    public async Task<string> GenerateProjectSummaryAsync(Project project)
    {
        try
        {
            var googleAi = new GoogleAI(_apiKey);
            var model = googleAi.GenerativeModel("gemini-1.5-pro");

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

            var response = await model.GenerateContent(prompt.ToString());
            return response.Text ?? "Failed to generate project summary.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating project summary with Gemini for Project {ProjectId}", project.Id);
            return "An error occurred while generating the project summary.";
        }
    }
}
