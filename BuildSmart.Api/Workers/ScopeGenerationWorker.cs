using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using System.Text.Json;
using System.Text;
using Hangfire;
using Polly;
using Microsoft.Extensions.Localization;
using BuildSmart.Core.Application.Resources;
using System.Globalization;
using Microsoft.AspNetCore.SignalR;

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
	public async Task ProcessJobAsync(Guid jobPostId, CancellationToken cancellationToken)
	{
		_logger.LogDebug("Processing Job Scope for Job ID: {JobId}", jobPostId);

		using var scope = _serviceProvider.CreateScope();
		var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
		var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();

		var jobPost = await unitOfWork.JobPosts.GetByIdAsync(jobPostId);
		if (jobPost == null)
		{
			_logger.LogWarning("Job Post {JobId} not found.", jobPostId);
			return;
		}

		// If the job was previously marked as Rejected due to a crash, reset it for the retry.
		if (jobPost.Status == JobPostStatus.Rejected)
		{
			jobPost.SubmitForScopeGeneration();
			unitOfWork.JobPosts.Update(jobPost);
			await unitOfWork.SaveChangesAsync();
		}

		try
		{
			var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
			var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<BuildSmart.Api.Hubs.JobProcessingHub>>();

			// Layer 1
			await hubContext.Clients.Group(jobPost.ProjectId.ToString()).SendAsync("ReceiveProcessingUpdate", 1, "Analyzing project requirements and building context...", 10);


			// 1. Build Human Readable Q&A Context
			var allCategories = await unitOfWork.ServiceCategories.GetAllAsync();
			var relevantCategories = allCategories.Where(c => c.Id == jobPost.ServiceCategoryId || c.IsGlobal).ToList();
			var humanReadableContext = BuildHumanReadableContext(jobPost.JobDetails, relevantCategories);

			// 2. Fetch Allowed SKUs for this Category
			var allowedSkus = (await unitOfWork.ServiceSkus.GetByCategoryAsync(jobPost.ServiceCategoryId)).ToList();

			// 2.5 Get Homeowner's Preferred Language
			string languageCode = "bg";
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

			var aiResponse = await retryPolicy.ExecuteAsync(async (ct) =>
			{
				await EnforceRateLimitAsync();

				return await aiService.GenerateJobScopeAsync(jobPost, humanReadableContext, allowedSkus, languageCode, ct);
			}, cancellationToken);

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

			_logger.LogDebug("Scope generated successfully for Job {JobId}.", jobPostId);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to generate scope for Job {JobId}.", jobPostId);

			try
			{
				using var errorScope = _serviceProvider.CreateScope();
				var errorUnitOfWork = errorScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
				var currentJobPost = await errorUnitOfWork.JobPosts.GetByIdAsync(jobPostId);
				if (currentJobPost != null)
				{
					currentJobPost.MarkGenerationFailed(ex.Message);
					errorUnitOfWork.JobPosts.Update(currentJobPost);
					await errorUnitOfWork.SaveChangesAsync();
				}
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
					contextBuilder.AppendLine($"ID: {qId}"); // Critical for AI mapping
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
	public async Task ProcessPricingAsync(Guid jobPostId, CancellationToken cancellationToken)
	{
		_logger.LogDebug("Processing Pricing for Job ID: {JobId}", jobPostId);

		using var scope = _serviceProvider.CreateScope();
		var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
		var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();
		var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

		var jobPost = await unitOfWork.JobPosts.GetByIdWithTasksAsync(jobPostId);
		if (jobPost == null)
		{
			_logger.LogWarning("Job Post {JobId} not found.", jobPostId);
			return;
		}

		try
		{
			_logger.LogDebug("Processing pricing for JobPost {JobId} with {TaskCount} tasks.", jobPostId, jobPost.JobTasks.Count);
			
			var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<BuildSmart.Api.Hubs.JobProcessingHub>>();

			// Layer 2
			await hubContext.Clients.Group(jobPost.ProjectId.ToString()).SendAsync("ReceiveProcessingUpdate", 2, "Matching materials and calculating pricing...", 35);

			var allCategories = await unitOfWork.ServiceCategories.GetAllAsync();
			var relevantCategories = allCategories.Where(c => c.Id == jobPost.ServiceCategoryId || c.IsGlobal).ToList();
			var humanReadableContext = BuildHumanReadableContext(jobPost.JobDetails, relevantCategories);

			// Fetch SKUs for specific category AND Global categories
			var allowedSkus = (await unitOfWork.ServiceSkus.GetByCategoryAsync(jobPost.ServiceCategoryId)).ToList();
			foreach (var globalCat in allCategories.Where(c => c.IsGlobal))
			{
				var globalSkus = await unitOfWork.ServiceSkus.GetByCategoryAsync(globalCat.Id);
				allowedSkus.AddRange(globalSkus);
			}

			if (allowedSkus.Count == 0)
			{
				_logger.LogWarning("No allowed SKUs found for JobPost {JobId} (Category: {CategoryId}). AI will not be able to map any prices.", jobPostId, jobPost.ServiceCategoryId);
			}
			else
			{
				_logger.LogDebug("Found {SkuCount} total allowed SKUs for pricing.", allowedSkus.Count);
			}

			// 2.5 Get Homeowner's Preferred Language
			string languageCode = "bg";
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
						_logger.LogWarning(exception, "Gemini API pricing failed on attempt {RetryCount}. Retrying in {Delay}s.", retryCount, timeSpan.TotalSeconds);
					});

			var aiResponse = await retryPolicy.ExecuteAsync(async (ct) =>
			{
				await EnforceRateLimitAsync();
				return await aiService.CalculateTaskPricesAsync(jobPost.JobTasks.ToList(), allowedSkus, humanReadableContext, languageCode, ct);
			}, cancellationToken);

			if (aiResponse?.Tasks != null)
			{
				using var saveScope = _serviceProvider.CreateScope();
				var saveUnitOfWork = saveScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
				var freshJobPost = await saveUnitOfWork.JobPosts.GetByIdWithTasksAsync(jobPostId);

				if (freshJobPost == null)
				{
					_logger.LogError("Could not find JobPost {JobId} during result saving phase.", jobPostId);
					return;
				}

				var aiCalc = await saveUnitOfWork.AiCalculations.GetByProjectAndCategoryAsync(freshJobPost.ProjectId, freshJobPost.ServiceCategoryId);
				if (aiCalc != null)
				{
					_logger.LogDebug("Deleting existing AiCalculation {AiCalcId} to refresh with new AI data.", aiCalc.Id);
					saveUnitOfWork.AiCalculations.Delete(aiCalc);
					await saveUnitOfWork.SaveChangesAsync();
				}

				aiCalc = new AiCalculation
				{
					Id = Guid.NewGuid(),
					ProjectId = freshJobPost.ProjectId,
					ServiceCategoryId = freshJobPost.ServiceCategoryId,
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				};
				await saveUnitOfWork.AiCalculations.AddAsync(aiCalc);

				decimal grandTotal = 0;

				foreach (var aiTask in aiResponse.Tasks)
				{
					if (!Guid.TryParse(aiTask.TaskId, out var parsedGuid))
					{
						_logger.LogWarning("AI returned invalid TaskId format: '{TaskId}'. Skipping task.", aiTask.TaskId);
						continue;
					}

					var matchedTask = freshJobPost.JobTasks.FirstOrDefault(t => t.Id == parsedGuid);
					if (matchedTask == null)
					{
						_logger.LogWarning("AI TaskId {TaskId} could not be matched with any existing JobTask for JobPost {JobId}.", aiTask.TaskId, jobPostId);
						continue;
					}

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

					if (aiTask.SkuItems != null && aiTask.SkuItems.Count > 0)
					{
						foreach (var skuDto in aiTask.SkuItems)
						{
							var matchedSku = allowedSkus.FirstOrDefault(s => s.SkuCode.Equals(skuDto.SkuCode, StringComparison.OrdinalIgnoreCase));
							if (matchedSku == null)
							{
								_logger.LogWarning("SKU {SkuCode} mapped by AI for Task {TaskId} was not found in the allowed list.", skuDto.SkuCode, parsedGuid);
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
							
							_logger.LogDebug("Mapped SKU {SkuCode} x {Quantity} to Task {TaskId}.", matchedSku.SkuCode, skuDto.Quantity, parsedGuid);
						}
					}
					else
					{
						_logger.LogWarning("Task {TaskId} ('{TaskTitle}') has no SKUs mapped by AI.", parsedGuid, matchedTask.Title);
					}

					calcTask.EstimatedPrice = taskTotal;
					grandTotal += taskTotal;
					aiCalc.Tasks.Add(calcTask);
				}

				aiCalc.TotalEstimatedPrice = grandTotal;

				// Layer 3
				await hubContext.Clients.Group(freshJobPost.ProjectId.ToString()).SendAsync("ReceiveProcessingUpdate", 3, "Validating estimations and finalizing structure...", 65);

				await saveUnitOfWork.SaveChangesAsync();
				_logger.LogDebug("Successfully saved AiCalculation for Job {JobId}. Grand Total: {GrandTotal}", jobPostId, grandTotal);

				// ==========================================
				// PDF AGGREGATION LOGIC (Incremental Updates)
				// ==========================================
				try
				{
					// Layer 4
					await hubContext.Clients.Group(freshJobPost.ProjectId.ToString()).SendAsync("ReceiveProcessingUpdate", 4, "Applying finishes and generating master offer...", 85);

					await GenerateMasterProjectPdf(freshJobPost.ProjectId, saveScope.ServiceProvider);
					
					// Layer 5
					await hubContext.Clients.Group(freshJobPost.ProjectId.ToString()).SendAsync("ReceiveProcessingUpdate", 5, "Complete", 100);
				}
				catch (Exception pdfEx)
				{
					_logger.LogError(pdfEx, "Failed to generate Master PDF for Project {ProjectId}", freshJobPost.ProjectId);
					
					var errorProject = await saveUnitOfWork.Projects.GetByIdAsync(freshJobPost.ProjectId);
					if (errorProject != null)
					{
						errorProject.GeneralSummary = "PDF ERROR: " + pdfEx.ToString();
						saveUnitOfWork.Projects.Update(errorProject);
						await saveUnitOfWork.SaveChangesAsync();
					}
				}
			}

			_logger.LogDebug("Pricing processed successfully for Job {JobId}.", jobPostId);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to process pricing for Job {JobId}.", jobPostId);

			try
			{
				using var errorScope = _serviceProvider.CreateScope();
				var errorUnitOfWork = errorScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
				var currentJobPost = await errorUnitOfWork.JobPosts.GetByIdAsync(jobPostId);
				if (currentJobPost != null)
				{
					currentJobPost.MarkGenerationFailed(ex.Message);
					errorUnitOfWork.JobPosts.Update(currentJobPost);
					await errorUnitOfWork.SaveChangesAsync();
				}
			}
			catch (Exception dbEx)
			{
				_logger.LogError(dbEx, "Failed to update JobPost status after pricing failure.");
			}

			throw; // Rethrow to ensure Hangfire registers the failure and triggers a retry
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
			var aiService = pdfScope.ServiceProvider.GetRequiredService<IAiService>();

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
						Amount = task.EstimatedPrice.ToString("N2"),
						AcceptanceCriteria = task.AcceptanceCriteria?.Select(c => c.Description).ToList() ?? new List<string>()
					}).ToList();

					categoriesData.Add(new
					{
						CategoryName = categoryName,
						Subtotal = calc.TotalEstimatedPrice.ToString("N2"),
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

				var combinedScope = new StringBuilder();
				foreach (var jp in projectWithUser.JobPosts.Where(j => !string.IsNullOrWhiteSpace(j.GeneratedScope)))
				{
					combinedScope.AppendLine($"## {jp.Title}");
					combinedScope.AppendLine(jp.GeneratedScope);
					combinedScope.AppendLine();
				}

				string finalScopeDescription = projectWithUser.Description;
				if (combinedScope.Length > 0)
				{
					await EnforceRateLimitAsync();
					finalScopeDescription = await aiService.GenerateExecutiveSummaryAsync(combinedScope.ToString(), projectWithUser.LanguageCode ?? "bg");
				}

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

					CurrencySymbol = "€",

					// Dynamic Data
					JobTitle = projectWithUser.Title,
					JobId = projectWithUser.Id.ToString().Substring(0, 8),
					TradesmanName = localizer["Label_SystemEstimate"].Value,
					Date = DateTime.UtcNow.ToString("MMM dd, yyyy"),
					ClientName = clientName,
					ClientAddress = clientAddress,
					ScopeDescription = finalScopeDescription,
					Categories = categoriesData,
					SubtotalAmount = grandTotal.ToString("N2"),
					TotalAmount = grandTotal.ToString("N2")
				};

				byte[] pdfBytes = await pdfService.GenerateOfferPdfAsync(offerData);

				project.MasterOfferPdf = pdfBytes;
				project.UpdatedAt = DateTime.UtcNow;
				unitOfWork.Projects.Update(project);
				await unitOfWork.SaveChangesAsync();

				_logger.LogDebug("Master PDF Generated for Project {ProjectId} and saved to database. Size: {Size} bytes", projectId, pdfBytes.Length);

				// Broadcast to Admin UI that the PDF is ready
				var hubContext = pdfScope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<BuildSmart.Api.Hubs.NotificationHub>>();
				await hubContext.Clients.All.SendAsync("OfferRegenerated", projectId);

				// Notify the Homeowner that the offer is fully complete
				var notificationService = pdfScope.ServiceProvider.GetRequiredService<INotificationService>();
				await notificationService.SendLocalizedNotificationAsync(
					projectWithUser.HomeownerId,
					"Title_OfferReady",
					"Msg_OfferReady",
					new object[] { projectWithUser.Title },
					projectWithUser.Id,
					"Project"
				);
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

	private async Task EnforceRateLimitAsync()
	{
		var timeSinceLastCall = UtcNowProvider() - _lastApiCallTime;
		if (timeSinceLastCall < TimeSpan.FromSeconds(3))
		{
			await DelayTask(TimeSpan.FromSeconds(3) - timeSinceLastCall);
		}
		_lastApiCallTime = UtcNowProvider();
	}
}