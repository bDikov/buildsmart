using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using System.Text.Json;
using System.Text;
using Hangfire;
using Polly;

namespace BuildSmart.Api.Workers;

public class ScopeGenerationWorker
{
	internal static DateTime _lastApiCallTime = DateTime.MinValue;
	internal static Func<TimeSpan, Task> DelayTask = Task.Delay;
	internal static Func<DateTime> UtcNowProvider = () => DateTime.UtcNow;

	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<ScopeGenerationWorker> _logger;

	public ScopeGenerationWorker(
		IServiceProvider serviceProvider,
		ILogger<ScopeGenerationWorker> logger)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	[AutomaticRetry(Attempts = 0)]
	[Queue("ai-queue")]
	public async Task ProcessJobAsync(Guid jobPostId)
	{
		_logger.LogInformation("Processing Job Scope for Job ID: {JobId}", jobPostId);

		using var scope = _serviceProvider.CreateScope();
		var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
		var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();

		var jobPost = await unitOfWork.JobPosts.GetByIdAsync(jobPostId);
		if (jobPost == null)
		{
			_logger.LogWarning("Job Post {JobId} not found.", jobPostId);
			return;
		}

		try
		{
			var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

			// 1. Build Human Readable Q&A Context
			var allCategories = await unitOfWork.ServiceCategories.GetAllAsync();
			var relevantCategories = allCategories.Where(c => c.Id == jobPost.ServiceCategoryId || c.IsGlobal).ToList();
			var humanReadableContext = BuildHumanReadableContext(jobPost.JobDetails, relevantCategories);

			// 2. Fetch Allowed SKUs for this Category
			var allowedSkus = (await unitOfWork.ServiceSkus.GetByCategoryAsync(jobPost.ServiceCategoryId)).ToList();

			// 3. Generate the Scope and Tasks using AI (with Polly Retry)
			var retryPolicy = Policy
				.Handle<Exception>()
				.WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(10 * retryAttempt),
					(exception, timeSpan, retryCount, context) =>
					{
						_logger.LogWarning(exception, "Gemini API failed on attempt {RetryCount}. Retrying in {Delay}s.", retryCount, timeSpan.TotalSeconds);
					});

			var aiResponse = await retryPolicy.ExecuteAsync(async () =>
			{
				var timeSinceLastCall = UtcNowProvider() - _lastApiCallTime;
				if (timeSinceLastCall < TimeSpan.FromSeconds(30))
				{
					await DelayTask(TimeSpan.FromSeconds(30) - timeSinceLastCall);
				}
				_lastApiCallTime = UtcNowProvider();

				return await aiService.GenerateJobScopeAsync(jobPost, humanReadableContext, allowedSkus);
			});

			// 4. Clear existing Tasks if any (in case of regeneration)
			var existingTasks = await unitOfWork.JobTasks.GetTasksByJobPostAsync(jobPostId);
			foreach (var t in existingTasks)
			{
				unitOfWork.JobTasks.Delete(t);
			}

			// 5. Create new JobTasks from AI Response and Calculate Price
			int sequence = 1;

			if (aiResponse.Tasks != null)
			{
				foreach (var item in aiResponse.Tasks)
				{
					var jobTask = new JobTask
					{
						Id = Guid.NewGuid(),
						JobPostId = jobPost.Id,
						Title = item.TaskTitle ?? "Untitled Task",
						Description = item.TaskDescription ?? string.Empty,
						SequenceOrder = sequence++,
						EstimatedPrice = 0,
						CreatedAt = DateTime.UtcNow,
						UpdatedAt = DateTime.UtcNow
					};

					if (item.SkuItems != null)
					{
						foreach (var skuDto in item.SkuItems)
						{
							if (string.IsNullOrWhiteSpace(skuDto.SkuCode)) continue;

							var matchedSku = allowedSkus.FirstOrDefault(s => s.SkuCode.Equals(skuDto.SkuCode, StringComparison.OrdinalIgnoreCase));
							if (matchedSku == null)
							{
								_logger.LogWarning("AI generated an unknown SKU Code: {SkuCode}. Skipping price mapping.", skuDto.SkuCode);
								continue;
							}

							var skuEstimatedPrice = matchedSku.BasePrice * skuDto.Quantity;
							jobTask.EstimatedPrice += skuEstimatedPrice;

							jobTask.SkuItems.Add(new TaskSkuItem
							{
								Id = Guid.NewGuid(),
								JobTaskId = jobTask.Id,
								ServiceSkuId = matchedSku.Id,
								Quantity = skuDto.Quantity,
								EstimatedPrice = skuEstimatedPrice,
								CreatedAt = DateTime.UtcNow,
								UpdatedAt = DateTime.UtcNow
							});
						}
					}

					if (item.AcceptanceCriteria != null)
					{
						foreach (var criteria in item.AcceptanceCriteria)
						{
							if (string.IsNullOrWhiteSpace(criteria)) continue;

							jobTask.AcceptanceCriteria.Add(new TaskAcceptanceCriteria
							{
								Id = Guid.NewGuid(),
								JobTaskId = jobTask.Id,
								Description = criteria,
								CreatedAt = DateTime.UtcNow,
								UpdatedAt = DateTime.UtcNow
							});
						}
					}

					await unitOfWork.JobTasks.AddAsync(jobTask);
				}
			}
			// 6. Update the Job Post
			jobPost.SetGeneratedScope(aiResponse.ScopeMarkdown);
			// Optionally store the total price somewhere on the jobPost.
			// The prompt asks to calculate it and return it to the frontend, which happens when the job is queried.

			// 7. Save Changes
			unitOfWork.JobPosts.Update(jobPost);
			await unitOfWork.SaveChangesAsync();

			// 8. Send Real-time Notification
			await notificationService.SendNotificationAsync(
				jobPost.Project.HomeownerId,
				"Scope Ready",
				$"The AI scope for '{jobPost.Title}' is ready for your review.",
				jobPost.Id,
				"JobPost"
			);

			_logger.LogInformation("Scope generated successfully for Job {JobId}.", jobPostId);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to generate scope for Job {JobId}.", jobPostId);

			try
			{
				jobPost.MarkGenerationFailed(ex.Message);
				unitOfWork.JobPosts.Update(jobPost);
				await unitOfWork.SaveChangesAsync();
			}
			catch (Exception dbEx)
			{
				_logger.LogError(dbEx, "Failed to update JobPost status after generation failure.");
			}
		}
	}

	private string BuildHumanReadableContext(string jobDetailsJson, List<ServiceCategory> categories)
	{
		if (string.IsNullOrWhiteSpace(jobDetailsJson) || jobDetailsJson == "{}")
			return "No answers provided.";

		var contextBuilder = new StringBuilder();
		var questionMap = new Dictionary<string, string>();

		foreach (var cat in categories)
		{
			if (string.IsNullOrWhiteSpace(cat.TemplateStructure) || cat.TemplateStructure == "{}") continue;

			try
			{
				using var templateDoc = JsonDocument.Parse(cat.TemplateStructure);
				if (templateDoc.RootElement.TryGetProperty("questions", out var questionsElement))
				{
					foreach (var q in questionsElement.EnumerateArray())
					{
						var qId = q.GetProperty("id").GetString();
						var qText = q.TryGetProperty("text", out var textProp) ? textProp.GetString() : "Unknown Question";
						if (!string.IsNullOrEmpty(qId) && !string.IsNullOrEmpty(qText))
						{
							questionMap[qId] = qText;
						}
					}
				}
			}
			catch (JsonException) { }
		}

		try
		{
			using var answersDoc = JsonDocument.Parse(jobDetailsJson);
			foreach (var answer in answersDoc.RootElement.EnumerateObject())
			{
				var qId = answer.Name;
				var ansVal = answer.Value.ValueKind == JsonValueKind.String ? answer.Value.GetString() : answer.Value.GetRawText();

				if (questionMap.TryGetValue(qId, out var qText))
				{
					contextBuilder.AppendLine($"Q: {qText}");
					contextBuilder.AppendLine($"A: {ansVal}");
					contextBuilder.AppendLine();
				}
				else
				{
					contextBuilder.AppendLine($"Q: {qId}");
					contextBuilder.AppendLine($"A: {ansVal}");
					contextBuilder.AppendLine();
				}
			}
		}
		catch (JsonException) { }

		return contextBuilder.ToString().Trim();
	}
}