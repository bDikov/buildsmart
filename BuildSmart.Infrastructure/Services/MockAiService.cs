using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using System.Text;

namespace BuildSmart.Infrastructure.Services;

public class MockAiService : IAiService
{
	public async Task<string> GenerateJobScopeAsync(JobPost jobPost)
	{
		// Simulate AI processing delay
		await Task.Delay(3000);

		var sb = new StringBuilder();
		sb.AppendLine($"## AI Generated Scope for: {jobPost.Title}");
		sb.AppendLine();
		sb.AppendLine("**Overview**");
		sb.AppendLine($"Based on your responses, we have outlined the following scope of work for your {jobPost.ServiceCategory.Name} project.");
		sb.AppendLine();
		sb.AppendLine("**Details**");
		sb.AppendLine($"- **Location:** {jobPost.Location}");
		sb.AppendLine($"- **Budget Estimation:** {jobPost.EstimatedBudget?.Total} {jobPost.EstimatedBudget?.Currency}");
		sb.AppendLine();
		sb.AppendLine("**Generated Requirements**");
		sb.AppendLine("1. Contractor to verify site conditions.");
		sb.AppendLine("2. Supply and install materials as specified in the questionnaire.");
		sb.AppendLine("3. Ensure compliance with local building codes.");
		sb.AppendLine("4. Clean up site upon completion.");
		sb.AppendLine();
		sb.AppendLine($"*(This is a mock AI response generated at {DateTime.UtcNow})*");

		return sb.ToString();
	}

	public async Task<string> GenerateProjectSummaryAsync(Project project)
	{
		await Task.Delay(2000);

		var sb = new StringBuilder();
		sb.AppendLine($"## Project Summary: {project.Title}");
		sb.AppendLine();
		sb.AppendLine($"This project consists of {project.JobPosts.Count} individual jobs.");
		sb.AppendLine("The homeowner is looking to coordinate multiple trades to complete a renovation at the property.");
		sb.AppendLine();
		sb.AppendLine("**Key Components:**");
		foreach (var j in project.JobPosts)
		{
			sb.AppendLine($"- {j.Title} ({j.ServiceCategory.Name})");
		}
		sb.AppendLine();
		sb.AppendLine("**Goal:**");
		sb.AppendLine("Complete all works with high quality and within the estimated timeline.");

		return sb.ToString();
	}
}