using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildSmart.Api.Workers;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BuildSmart.Api.Tests
{
	public class ScopeGenerationWorkerIntegrationTests : IClassFixture<TestApplicationFactory>
	{
		private readonly TestApplicationFactory _factory;

		public ScopeGenerationWorkerIntegrationTests(TestApplicationFactory factory)
		{
			_factory = factory;
		}

		[Fact(Skip = "Run manually to test real Gemini AI API. Requires 'Gemini:ApiKey' configured in user secrets.")]
		public async Task TestRealAiScopeGenerationAndDatabasePersistence()		{
			// 1. Create a service scope
			using var scope = _factory.Services.CreateScope();
			var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
			var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

			// We force injecting the REAL GeminiAiService here, ignoring the MockAiService from Program.cs
			var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
			var aiService = new GeminiAiService(config, loggerFactory.CreateLogger<GeminiAiService>());

			// 2. Seed the Database with required mock data

			// Homeowner User
			var homeowner = new User
			{
				Id = Guid.NewGuid(),
				Email = $"test-ai-{Guid.NewGuid()}@bs.com",
				FirstName = "Test",
				LastName = "Homeowner",
				HashedPassword = "hash",
				Role = UserRoleTypes.Homeowner
			};

			var homeownerProfile = new HomeownerProfile { Id = Guid.NewGuid(), UserId = homeowner.Id };
			homeowner.HomeownerProfile = homeownerProfile;
			await dbContext.Users.AddAsync(homeowner);

			// Category & Template
			var category = new ServiceCategory 
			{ 
			    Id = Guid.NewGuid(), 
			    Name = $"Complex Electrical Renovation {Guid.NewGuid()}",
			    TemplateStructure = "{\"questions\": [" +
			                        "{\"id\": \"q1\", \"text\": \"What type of walls are in the room? (e.g., Drywall, Solid Brick, Concrete)\"}, " +
			                        "{\"id\": \"q2\", \"text\": \"Are we installing new outlets, moving existing ones, or replacing old wiring?\"}, " +
			                        "{\"id\": \"q3\", \"text\": \"How many standard 120V outlets do you need in total?\"}, " +
			                        "{\"id\": \"q4\", \"text\": \"What type of light switches are you installing? (Standard or Smart/WiFi)\"}, " +
			                        "{\"id\": \"q5\", \"text\": \"How many switches?\"}, " +
			                        "{\"id\": \"q6\", \"text\": \"Will this require a new dedicated circuit at the breaker panel?\"}" +
			                        "]}"
			};
			await dbContext.ServiceCategories.AddAsync(category);

			// Complex SKUs Menu
			var skus = new List<ServiceSku>
			{
			    new ServiceSku { Id = Guid.NewGuid(), ServiceCategoryId = category.Id, SkuCode = "ELEC_OUTLET_NEW", Name = "Install New 120V Outlet", BasePrice = 120m, UnitType = "Per Item" },
			    new ServiceSku { Id = Guid.NewGuid(), ServiceCategoryId = category.Id, SkuCode = "ELEC_OUTLET_MOVE", Name = "Move Existing Outlet", BasePrice = 85m, UnitType = "Per Item" },

			    new ServiceSku { Id = Guid.NewGuid(), ServiceCategoryId = category.Id, SkuCode = "SWITCH_STD", Name = "Install Standard Switch", BasePrice = 95m, UnitType = "Per Item" },
			    new ServiceSku { Id = Guid.NewGuid(), ServiceCategoryId = category.Id, SkuCode = "SWITCH_SMART", Name = "Install Smart Switch (Requires Neutral)", BasePrice = 160m, UnitType = "Per Item" },

			    new ServiceSku { Id = Guid.NewGuid(), ServiceCategoryId = category.Id, SkuCode = "WIRE_PULL_DRYWALL", Name = "Fish Wire Through Drywall", BasePrice = 150m, UnitType = "Per Room" },
			    new ServiceSku { Id = Guid.NewGuid(), ServiceCategoryId = category.Id, SkuCode = "CHASE_BRICK", Name = "Wall Chasing (Brick)", Description = "Cutting channels into solid brick", BasePrice = 300m, UnitType = "Per Room" },
			    new ServiceSku { Id = Guid.NewGuid(), ServiceCategoryId = category.Id, SkuCode = "CHASE_CONCRETE", Name = "Wall Chasing (Concrete)", Description = "Heavy cutting/drilling into solid concrete", BasePrice = 500m, UnitType = "Per Room" },

			    new ServiceSku { Id = Guid.NewGuid(), ServiceCategoryId = category.Id, SkuCode = "WIRE_REPLACE_OLD", Name = "Rip & Replace Old Wiring", BasePrice = 350m, UnitType = "Per Room" },
			    new ServiceSku { Id = Guid.NewGuid(), ServiceCategoryId = category.Id, SkuCode = "WIRE_ADD_NEUTRAL", Name = "Run Neutral Wire for Smart Devices", BasePrice = 200m, UnitType = "Per Circuit" },

			    new ServiceSku { Id = Guid.NewGuid(), ServiceCategoryId = category.Id, SkuCode = "ELEC_NEW_CIRCUIT", Name = "Add 20A Dedicated Circuit", BasePrice = 450m, UnitType = "Flat" },
			    new ServiceSku { Id = Guid.NewGuid(), ServiceCategoryId = category.Id, SkuCode = "DRYWALL_PATCH", Name = "Patch Drywall", BasePrice = 200m, UnitType = "Per Room" }
			};

			await dbContext.ServiceSkus.AddRangeAsync(skus);

			// Project & JobPost
			var project = new Project { Id = Guid.NewGuid(), Title = "Living Room Tech Upgrade", Description = "Updating an old 1950s living room for modern tech.", HomeownerId = homeowner.Id };
			await dbContext.Projects.AddAsync(project);

			var jobDetailsJson = "{" +
			                     "\"q1\": \"The walls are solid brick from the 1950s.\", " +
			                     "\"q2\": \"The old wiring is scary so we need to replace it all. I also want to move 2 of the existing outlets to different walls.\", " +
			                     "\"q3\": \"I need 4 outlets in total for the room.\", " +
			                     "\"q4\": \"We are putting in Caseta Smart WiFi switches everywhere.\", " +
			                     "\"q5\": \"We need 3 switches.\", " +
			                     "\"q6\": \"No, the panel was upgraded last year, plenty of room on the existing circuit.\"" +
			                     "}";

			var jobPost = new JobPost
			{
			    Id = Guid.NewGuid(),
			    ProjectId = project.Id,
			    ServiceCategoryId = category.Id,
			    Title = "Rewire Living Room & Smart Switches",
			    Description = "Complex rewire in a brick room.",
			    Location = "Living Room",
			    JobDetails = jobDetailsJson,
			    HomeownerProfileId = homeownerProfile.Id
			};
			await dbContext.JobPosts.AddAsync(jobPost);

			await dbContext.SaveChangesAsync(); // Commit setup to DB

			// 3. PHASE 1: DRAFTING
			string humanReadableContext = $"Q: What type of walls are in the room? (e.g., Drywall, Solid Brick, Concrete)\nA: The walls are solid brick from the 1950s.\n\n" +
			                              $"Q: Are we installing new outlets, moving existing ones, or replacing old wiring?\nA: The old wiring is scary so we need to replace it all. I also want to move 2 of the existing outlets to different walls.\n\n" +
			                              $"Q: How many standard 120V outlets do you need in total?\nA: I need 4 outlets in total for the room.\n\n" +
			                              $"Q: What type of light switches are you installing? (Standard or Smart/WiFi)\nA: We are putting in Caseta Smart WiFi switches everywhere.\n\n" +
			                              $"Q: How many switches?\nA: We need 3 switches.\n\n" +
			                              $"Q: Will this require a new dedicated circuit at the breaker panel?\nA: No, the panel was upgraded last year, plenty of room on the existing circuit.";

			jobPost.SubmitForScopeGeneration();
			var aiResponse = await aiService.GenerateJobScopeAsync(jobPost, humanReadableContext, skus);
			
			// Assert Drafting works
			Assert.NotNull(aiResponse);
			Assert.NotEmpty(aiResponse.ScopeMarkdown);
			Assert.NotEmpty(aiResponse.Tasks);

			// Convert DTOs to DB Entities with $0.00 estimated price
			var tasks = new List<JobTask>();
			foreach (var item in aiResponse.Tasks)
			{
				var jobTask = new JobTask
				{
					Id = Guid.NewGuid(),
					JobPostId = jobPost.Id,
					Title = item.TaskTitle ?? "Untitled",
					Description = item.TaskDescription ?? string.Empty,
					EstimatedPrice = 0 // Started at $0
				};

				if (item.AcceptanceCriteria != null)
				{
					foreach(var c in item.AcceptanceCriteria)
					{
						jobTask.AcceptanceCriteria.Add(new TaskAcceptanceCriteria { Id = Guid.NewGuid(), Description = c });
					}
				}
				tasks.Add(jobTask);
			}

			// 4. PHASE 2: PRICING (Simulating clicking Generate Offer)
			var aiPricingResponse = await aiService.CalculateTaskPricesAsync(tasks, skus, "dummy context", "en");
			
			Assert.NotNull(aiPricingResponse);
			Assert.NotEmpty(aiPricingResponse.Tasks);

			decimal totalJobPrice = 0;
			
			foreach(var aiTask in aiPricingResponse.Tasks)
			{
				var matchedTask = tasks.FirstOrDefault(t => t.Title == aiTask.TaskTitle);
				Assert.NotNull(matchedTask);

				foreach (var skuDto in aiTask.SkuItems)
				{
					var matchedSku = skus.FirstOrDefault(s => s.SkuCode == skuDto.SkuCode);
					if (matchedSku != null)
					{
						var price = matchedSku.BasePrice * skuDto.Quantity;
						matchedTask.EstimatedPrice += price;
						matchedTask.SkuItems.Add(new TaskSkuItem
						{
							Id = Guid.NewGuid(),
							ServiceSkuId = matchedSku.Id,
							Quantity = skuDto.Quantity,
							EstimatedPrice = price
						});
					}
				}
				totalJobPrice += matchedTask.EstimatedPrice;
				await dbContext.JobTasks.AddAsync(matchedTask);
			}

			jobPost.SetGeneratedScope(aiResponse.ScopeMarkdown);
			await dbContext.SaveChangesAsync();

			// 5. Final Asserts & Logging
			var savedTasks = dbContext.JobTasks.Where(t => t.JobPostId == jobPost.Id).ToList();
			Assert.True(savedTasks.Count > 0);
			Assert.True(totalJobPrice > 0, "The AI should have successfully mapped SKUs and calculated a price > $0.00.");

			var logger = loggerFactory.CreateLogger<ScopeGenerationWorkerIntegrationTests>();
			logger.LogInformation("--- AI SCOPE MARKDOWN ---");
			logger.LogInformation(aiResponse.ScopeMarkdown);

			foreach (var t in savedTasks)
			{
				logger.LogInformation($"Task: {t.Title} - Estimated Price: ${t.EstimatedPrice}");
				var savedSkus = dbContext.TaskSkuItems.Where(ts => ts.JobTaskId == t.Id).ToList();
				foreach (var s in savedSkus)
				{
					var matchedSkuName = skus.First(sk => sk.Id == s.ServiceSkuId).Name;
					logger.LogInformation($"  - SKU: {matchedSkuName} | Qty: {s.Quantity} | Price: ${s.EstimatedPrice}");
				}
			}
		}

		[Fact(Skip = "Run manually to test real Gemini AI API. Requires 'Gemini:ApiKey' configured in user secrets.")]
		public async Task TestLanguageEnforcement_PassesCorrectCodeToAI()
		{
			using var scope = _factory.Services.CreateScope();
			var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
			var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
			var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
			var aiService = new GeminiAiService(config, loggerFactory.CreateLogger<GeminiAiService>());

			// Use Bulgarian as preferred language
			var homeowner = new User
			{
				Id = Guid.NewGuid(),
				Email = $"test-lang-{Guid.NewGuid()}@bs.com",
				FirstName = "Test",
				LastName = "Homeowner",
				HashedPassword = "hash",
				Role = UserRoleTypes.Homeowner,
				PreferredLanguage = "bg"
			};
			var homeownerProfile = new HomeownerProfile { Id = Guid.NewGuid(), UserId = homeowner.Id };
			homeowner.HomeownerProfile = homeownerProfile;
			await dbContext.Users.AddAsync(homeowner);

			var category = new ServiceCategory 
			{ 
			    Id = Guid.NewGuid(), 
			    Name = $"Simple Paint {Guid.NewGuid()}",
			    TemplateStructure = "{\"questions\": [{\"id\": \"q1\", \"text\": \"What color?\"}]}"
			};
			await dbContext.ServiceCategories.AddAsync(category);

			var skus = new List<ServiceSku>
			{
			    new ServiceSku { Id = Guid.NewGuid(), ServiceCategoryId = category.Id, SkuCode = "PAINT", Name = "Painting", BasePrice = 10m, UnitType = "Per Sqm" }
			};
			await dbContext.ServiceSkus.AddRangeAsync(skus);

			var project = new Project { Id = Guid.NewGuid(), Title = "Paint Room", Description = "Paint it.", HomeownerId = homeowner.Id, LanguageCode = "bg" };
			await dbContext.Projects.AddAsync(project);

			var jobDetailsJson = "{\"q1\": \"Blue\"}";

			var jobPost = new JobPost
			{
			    Id = Guid.NewGuid(),
			    ProjectId = project.Id,
			    ServiceCategoryId = category.Id,
			    Title = "Painting",
			    Description = "Blue Paint",
			    Location = "Living Room",
			    JobDetails = jobDetailsJson,
			    HomeownerProfileId = homeownerProfile.Id
			};
			await dbContext.JobPosts.AddAsync(jobPost);
			await dbContext.SaveChangesAsync();

			string humanReadableContext = $"Q: What color?\nA: Blue.";

			jobPost.SubmitForScopeGeneration();
			
			// ACT: Call AI Service with LanguageCode = bg
			var aiResponse = await aiService.GenerateJobScopeAsync(jobPost, humanReadableContext, skus, "bg");
			
			// ASSERT: Check that AI correctly returned Cyrillic/Bulgarian text
			Assert.NotNull(aiResponse);
			Assert.NotEmpty(aiResponse.ScopeMarkdown);
			
			// A naive but effective check to ensure Bulgarian was generated instead of English
			bool containsCyrillic = aiResponse.ScopeMarkdown.Any(c => c >= '\u0400' && c <= '\u04FF');
			Assert.True(containsCyrillic, "AI Response should contain Cyrillic characters because language code 'bg' was enforced.");
		}
	}
}