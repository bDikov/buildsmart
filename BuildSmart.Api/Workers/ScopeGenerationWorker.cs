using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using System.Text.Json;
using System.Text;
using Hangfire;
using Polly;
using Microsoft.Extensions.Localization;
using BuildSmart.Core.Application.Resources;
using System.Globalization;

namespace BuildSmart.Api.Workers;

public class ScopeGenerationWorker
{
	internal static DateTime _lastApiCallTime = DateTime.MinValue;
	internal static Func<TimeSpan, Task> DelayTask = Task.Delay;
	internal static Func<DateTime> UtcNowProvider = () => DateTime.UtcNow;
	private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, SemaphoreSlim> _pdfLocks = new();

	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<ScopeGenerationWorker> _logger;

	public ScopeGenerationWorker(
		IServiceProvider serviceProvider,
		ILogger<ScopeGenerationWorker> logger)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	[AutomaticRetry(Attempts = 2)]
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

			// 2.5 Get Homeowner's Preferred Language
			string languageCode = "en";
			if (jobPost.Project != null)
			{
				var homeowner = await unitOfWork.Users.GetByIdAsync(jobPost.Project.HomeownerId);
				if (homeowner != null && !string.IsNullOrWhiteSpace(homeowner.PreferredLanguage))
				{
					languageCode = homeowner.PreferredLanguage;
				}
			}

			// 3. Generate the Scope and Tasks using AI (with Polly Retry)
			var retryPolicy = Policy
				.Handle<Exception>()
				.WaitAndRetryAsync(4, retryAttempt => TimeSpan.FromSeconds(5 * retryAttempt),
					(exception, timeSpan, retryCount, context) =>
					{
						_logger.LogWarning(exception, "Gemini API failed on attempt {RetryCount}. Retrying in {Delay}s.", retryCount, timeSpan.TotalSeconds);
					});

			var aiResponse = await retryPolicy.ExecuteAsync(async () =>
			{
				_lastApiCallTime = UtcNowProvider();

				return await aiService.GenerateJobScopeAsync(jobPost, humanReadableContext, allowedSkus, languageCode);
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

			// 7.5 Automatically trigger pricing calculation so the user doesn't have to wait or click anything else
			var scopeGenerationQueue = scope.ServiceProvider.GetRequiredService<IScopeGenerationQueue>();
			await scopeGenerationQueue.QueuePricingUpdateAsync(jobPostId, CancellationToken.None);

			await notificationService.SendLocalizedNotificationAsync(
				jobPost.Project.HomeownerId,
				"Title_ScopeReady",
				"Msg_ScopeReady",
				new object[] { jobPost.Title },
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
				// Strict Backend Filtering: If the question ID is not found in the relevant (Global + Specific) categories,
				// we intentionally IGNORE it. This guarantees the AI never sees answers from other trades (e.g. Electrical answers leaking into Plumbing).
			}
		}
		catch (JsonException) { }

		return contextBuilder.ToString().Trim();
	}

	[AutomaticRetry(Attempts = 3)]
	[Queue("ai-queue")]
	public async Task ProcessPricingAsync(Guid jobPostId)
	{
		_logger.LogInformation("Processing Pricing for Job ID: {JobId}", jobPostId);
		string debugLogFile = @"C:\Users\bonch\source\repos\worker_debug.txt";
		System.IO.File.AppendAllText(debugLogFile, $"\n\n--- Started ProcessPricingAsync for {jobPostId} at {DateTime.Now} ---\n");

		using var scope = _serviceProvider.CreateScope();
		var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
		var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();
		var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

		var jobPost = await unitOfWork.JobPosts.GetByIdWithTasksAsync(jobPostId);
		if (jobPost == null)
		{
			_logger.LogWarning("Job Post {JobId} not found.", jobPostId);
			System.IO.File.AppendAllText(debugLogFile, $"JobPost {jobPostId} not found.\n");
			return;
		}

		try
		{
			System.IO.File.AppendAllText(debugLogFile, $"Found JobPost with {jobPost.JobTasks.Count} tasks.\n");
			var allowedSkus = (await unitOfWork.ServiceSkus.GetByCategoryAsync(jobPost.ServiceCategoryId)).ToList();
			System.IO.File.AppendAllText(debugLogFile, $"Found {allowedSkus.Count} allowed SKUs.\n");

			// 2.5 Get Homeowner's Preferred Language
			string languageCode = "en";
			if (jobPost.Project != null)
			{
				var homeowner = await unitOfWork.Users.GetByIdAsync(jobPost.Project.HomeownerId);
				if (homeowner != null && !string.IsNullOrWhiteSpace(homeowner.PreferredLanguage))
				{
					languageCode = homeowner.PreferredLanguage;
				}
			}

			var retryPolicy = Policy
				.Handle<Exception>()
				.WaitAndRetryAsync(4, retryAttempt => TimeSpan.FromSeconds(5 * retryAttempt),
					(exception, timeSpan, retryCount, context) =>
					{
						_logger.LogWarning(exception, "Gemini API failed on attempt {RetryCount}. Retrying in {Delay}s.", retryCount, timeSpan.TotalSeconds);
					});

			var aiResponse = await retryPolicy.ExecuteAsync(async () =>
			{
				_lastApiCallTime = UtcNowProvider();

				System.IO.File.AppendAllText(debugLogFile, $"Calling AI Service...\n");
				return await aiService.CalculateTaskPricesAsync(jobPost.JobTasks.ToList(), allowedSkus, languageCode);
			});

			System.IO.File.AppendAllText(debugLogFile, $"AI Service returned. Tasks count: {aiResponse?.Tasks?.Count ?? 0}\n");

			if (aiResponse.Tasks != null)
			{
				using var saveScope = _serviceProvider.CreateScope();
				var saveUnitOfWork = saveScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
				var freshJobPost = await saveUnitOfWork.JobPosts.GetByIdWithTasksAsync(jobPostId);

				if (freshJobPost == null)
				{
					System.IO.File.AppendAllText(debugLogFile, $"freshJobPost is null.\n");
					return;
				}

				var aiCalc = await saveUnitOfWork.AiCalculations.GetByProjectAndCategoryAsync(freshJobPost.ProjectId, freshJobPost.ServiceCategoryId);
				if (aiCalc == null)
				{
					System.IO.File.AppendAllText(debugLogFile, $"Creating new AiCalculation.\n");
					aiCalc = new AiCalculation
					{
						Id = Guid.NewGuid(),
						ProjectId = freshJobPost.ProjectId,
						ServiceCategoryId = freshJobPost.ServiceCategoryId,
						CreatedAt = DateTime.UtcNow,
						UpdatedAt = DateTime.UtcNow
					};
					await saveUnitOfWork.AiCalculations.AddAsync(aiCalc);
				}
				else
				{
					System.IO.File.AppendAllText(debugLogFile, $"Updating existing AiCalculation {aiCalc.Id}.\n");
					saveUnitOfWork.AiCalculations.RemoveTasks(aiCalc.Tasks.ToList());
					aiCalc.Tasks.Clear();
					aiCalc.UpdatedAt = DateTime.UtcNow;
				}

				decimal grandTotal = 0;

				foreach (var aiTask in aiResponse.Tasks)
				{
					var matchedTask = freshJobPost.JobTasks.FirstOrDefault(t => t.Id == aiTask.TaskId);
					if (matchedTask == null)
					{
						System.IO.File.AppendAllText(debugLogFile, $"WARN: Could not match AI TaskId {aiTask.TaskId} with any JobTask.\n");
						continue;
					}

					System.IO.File.AppendAllText(debugLogFile, $"Matched JobTask {matchedTask.Id} ('{matchedTask.Title}').\n");

					decimal taskTotal = 0;
					var calcTask = new AiCalculationTask
					{
						Id = Guid.NewGuid(),
						AiCalculationId = aiCalc.Id,
						Title = matchedTask.Title,
						Description = matchedTask.Description,
						SequenceOrder = matchedTask.SequenceOrder,
						EstimatedPrice = 0
					};

					foreach (var c in matchedTask.AcceptanceCriteria)
					{
						calcTask.AcceptanceCriteria.Add(new AiCalculationCriteria
						{
							Id = Guid.NewGuid(),
							AiCalculationTaskId = calcTask.Id,
							Description = c.Description
						});
					}

					if (aiTask.SkuItems != null)
					{
						foreach (var skuDto in aiTask.SkuItems)
						{
							var matchedSku = allowedSkus.FirstOrDefault(s => s.SkuCode.Equals(skuDto.SkuCode, StringComparison.OrdinalIgnoreCase));
							if (matchedSku == null)
							{
								System.IO.File.AppendAllText(debugLogFile, $"WARN: SKU {skuDto.SkuCode} not found in allowed SKUs.\n");
								continue;
							}

							var skuEstimatedPrice = matchedSku.BasePrice * skuDto.Quantity;
							taskTotal += skuEstimatedPrice;

							calcTask.SkuItems.Add(new AiCalculationSkuItem
							{
								Id = Guid.NewGuid(),
								AiCalculationTaskId = calcTask.Id,
								ServiceSkuId = matchedSku.Id,
								Quantity = skuDto.Quantity,
								EstimatedPrice = skuEstimatedPrice
							});
							System.IO.File.AppendAllText(debugLogFile, $"Mapped SKU {matchedSku.SkuCode} x {skuDto.Quantity}.\n");
						}
					}

					calcTask.EstimatedPrice = taskTotal;
					grandTotal += taskTotal;
					aiCalc.Tasks.Add(calcTask);
				}

				aiCalc.TotalEstimatedPrice = grandTotal;

				await saveUnitOfWork.SaveChangesAsync();
				System.IO.File.AppendAllText(debugLogFile, $"Successfully saved AiCalculation to DB. Grand Total: {grandTotal}\n");

				// ==========================================
				// PDF AGGREGATION LOGIC (Incremental Updates)
				// ==========================================
				try
				{
					await GenerateMasterProjectPdf(freshJobPost.ProjectId, saveScope.ServiceProvider);
				}
				catch (Exception pdfEx)
				{
					_logger.LogError(pdfEx, "Failed to generate Master PDF for Project {ProjectId}", freshJobPost.ProjectId);
				}
			}

			if (jobPost.Project != null)
			{
				await notificationService.SendLocalizedNotificationAsync(
					jobPost.Project.HomeownerId,
					"Title_PricingUpdated",
					"Msg_PricingUpdated",
					new object[] { jobPost.Title },
					jobPost.Id,
					"JobPost"
				);
				System.IO.File.AppendAllText(debugLogFile, $"Sent SignalR notification.\n");
			}

			_logger.LogInformation("Pricing processed successfully for Job {JobId}.", jobPostId);
		}
		catch (Exception ex)
		{
			System.IO.File.AppendAllText(debugLogFile, $"EXCEPTION: {ex.Message}\n{ex.StackTrace}\n");
			_logger.LogError(ex, "Failed to process pricing for Job {JobId}.", jobPostId);

			try
			{
				jobPost.MarkGenerationFailed(ex.Message);
				unitOfWork.JobPosts.Update(jobPost);
				await unitOfWork.SaveChangesAsync();
			}
			catch (Exception dbEx)
			{
				_logger.LogError(dbEx, "Failed to update JobPost status after pricing failure.");
			}
		}
	}

	private async Task GenerateMasterProjectPdf(Guid projectId, IServiceProvider serviceProvider)
	{
		var pdfLock = _pdfLocks.GetOrAdd(projectId, _ => new SemaphoreSlim(1, 1));
		await pdfLock.WaitAsync();

		try
		{
			// We MUST create a fresh scope and UoW INSIDE the lock to ensure we load the latest version of the Project entity
			using var pdfScope = serviceProvider.CreateScope();
			var unitOfWork = pdfScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
			var pdfService = pdfScope.ServiceProvider.GetRequiredService<IPdfGeneratorService>();
			var localizer = pdfScope.ServiceProvider.GetRequiredService<IStringLocalizer<OfferResources>>();

			var project = await unitOfWork.Projects.GetByIdAsync(projectId);
			if (project == null) return;

			// Store original culture and safely switch to project language
			var originalCulture = CultureInfo.CurrentUICulture;
			try
			{
				try
				{
					CultureInfo.CurrentUICulture = new CultureInfo(project.LanguageCode ?? "en");
				}
				catch (CultureNotFoundException)
				{
					CultureInfo.CurrentUICulture = new CultureInfo("en");
				}

				var allCalculations = await unitOfWork.AiCalculations.GetByProjectWithTasksAsync(projectId);

				var categoriesData = new List<dynamic>();
				decimal grandTotal = 0;

				string currencySymbol = CultureInfo.CurrentUICulture.Name.StartsWith("bg") ? "€" : "$";

				foreach (var calc in allCalculations)
				{
					var category = await unitOfWork.ServiceCategories.GetByIdAsync(calc.ServiceCategoryId);
					var categoryName = category?.Name ?? "General";

					grandTotal += calc.TotalEstimatedPrice;

					var tasksForCategory = calc.Tasks.OrderBy(t => t.SequenceOrder).Select(task => new
					{
						Description = task.Title,
						Amount = task.EstimatedPrice.ToString("F2"),
						AcceptanceCriteria = task.AcceptanceCriteria?.Select(c => c.Description).ToList() ?? new List<string>()
					}).ToList();

					categoriesData.Add(new
					{
						CategoryName = categoryName,
						Subtotal = calc.TotalEstimatedPrice.ToString("F2"),
						SubtotalLabel = string.Format(localizer["Label_Subtotal"].Value, categoryName),
						Tasks = tasksForCategory
					});
				}

				// Fetch project with Homeowner details
				var projectWithUser = await unitOfWork.Projects.GetByIdAsync(projectId);
				if (projectWithUser == null) return;
				
				// Ensure Homeowner is loaded (assuming the repository handles or we load it here)
				var homeowner = await unitOfWork.Users.GetByIdAsync(projectWithUser.HomeownerId);
				var clientName = homeowner != null ? $"{homeowner.FirstName} {homeowner.LastName}" : "Valued Client";
				
				// Location is stored on the JobPosts, not the Project entity
				var clientAddress = projectWithUser.JobPosts.FirstOrDefault()?.Location ?? homeowner?.Location ?? "TBD";

				var offerData = new
				{
					// Localization Headers
					Header_Hello = localizer["Header_Hello"].Value,
					Header_ProjectProposal = localizer["Header_ProjectProposal"].Value,
					Header_Overview = localizer["Header_Overview"].Value,
					Header_PreparedFor = localizer["Header_PreparedFor"].Value,
					Header_Fees = localizer["Header_Fees"].Value,
					Label_FeesDescription = localizer["Label_FeesDescription"].Value,
					Label_GrandTotal = localizer["Label_GrandTotal"].Value,
					Header_Terms = localizer["Header_Terms"].Value,

					Terms_Intro = localizer["Terms_Intro"].Value,
					Terms_Point1 = localizer["Terms_Point1"].Value,
					Terms_Point2 = localizer["Terms_Point2"].Value,
					Terms_Point3 = localizer["Terms_Point3"].Value,
					Terms_Point4 = localizer["Terms_Point4"].Value,
					Terms_Point5 = localizer["Terms_Point5"].Value,

					Footer_Validity = localizer["Footer_Validity"].Value,
					Label_ProjectBrief = localizer["Label_ProjectBrief"].Value,
					Label_PricingBreakdown = localizer["Label_PricingBreakdown"].Value,
					Label_TC = localizer["Label_TC"].Value,

					CurrencySymbol = currencySymbol,

					// Dynamic Data
					JobTitle = projectWithUser.Title,
					JobId = projectWithUser.Id.ToString().Substring(0, 8),
					TradesmanName = localizer["Label_SystemEstimate"].Value,
					Date = DateTime.UtcNow.ToString("MMM dd, yyyy"),
					ClientName = clientName,
					ClientAddress = clientAddress,
					ScopeDescription = projectWithUser.Description,
					Categories = categoriesData,
					SubtotalAmount = grandTotal.ToString("F2"),
					TotalAmount = grandTotal.ToString("F2")
				};

				byte[] pdfBytes = await pdfService.GenerateOfferPdfAsync(offerData);

				project.MasterOfferPdf = pdfBytes;
				project.UpdatedAt = DateTime.UtcNow;
				unitOfWork.Projects.Update(project);
				await unitOfWork.SaveChangesAsync();

				_logger.LogInformation("Master PDF Generated for Project {ProjectId} and saved to database. Size: {Size} bytes", projectId, pdfBytes.Length);
			}
			finally
			{
				CultureInfo.CurrentUICulture = originalCulture;
			}
		}
		finally
		{
			pdfLock.Release();
		}
	}
}