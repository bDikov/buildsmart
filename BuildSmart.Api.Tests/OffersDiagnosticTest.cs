using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Infrastructure.Persistence.Repositories;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Infrastructure.Services;
using BuildSmart.Api.Workers;
using BuildSmart.Api.Hubs;
using BuildSmart.Api.Services;

namespace BuildSmart.Api.Tests
{
    public class OffersDiagnosticTest
    {
        [Fact(Skip = "Local diagnostics only")]
        public async Task DiagnoseOfferGeneration()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Starting direct Hangfire execution diagnostics...");

            try
            {
                // Setup services
                var services = new ServiceCollection();

                // 1. DbContext
                string connString = "Server=localhost;Port=5432;Database=buildsmart_db;Username=postgres;Password=postgres";
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseNpgsql(connString);
                });

                // 2. Repositories & UOW
                services.AddScoped<IUnitOfWork, UnitOfWork>();
                services.AddScoped<IServiceCategoryRepository, ServiceCategoryRepository>();
                services.AddScoped<IJobPostRepository, JobPostRepository>();
                services.AddScoped<IUserRepository, UserRepository>();
                services.AddScoped<INotificationRepository, NotificationRepository>();

                // 3. Configuration with ApiKey
                var configuration = new ConfigurationBuilder().Build();
                var configMock = new Mock<IConfiguration>();
                configMock.Setup(c => c["Gemini:ApiKey"]).Returns(Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "PLACEHOLDER_KEY");
                services.AddSingleton<IConfiguration>(configMock.Object);

                // 4. Logger
                services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(builder => { }));
                services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

                // 5. AI Service & Pricing Engine
                services.AddScoped<IAiService, GeminiAiService>();
                services.AddScoped<IPricingEngine, PricingEngine>();

                // 6. SignalR Hub mock
                var mockHubContext = new Mock<IHubContext<JobProcessingHub>>();
                var mockClients = new Mock<IHubClients>();
                var mockClientProxy = new Mock<IClientProxy>();
                mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
                mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
                services.AddSingleton<IHubContext<JobProcessingHub>>(mockHubContext.Object);

                // 7. Notification & Queue mocks
                var mockNotification = new Mock<INotificationService>();
                services.AddSingleton<INotificationService>(mockNotification.Object);

                var mockQueue = new Mock<IScopeGenerationQueue>();
                services.AddSingleton<IScopeGenerationQueue>(mockQueue.Object);

                // 8. ScopeGenerationWorker
                services.AddScoped<ScopeGenerationWorker>();

                var serviceProvider = services.BuildServiceProvider();

                // Run Layer 1 & Layer 2 manually
                using (var testScope = serviceProvider.CreateScope())
                {
                    var worker = testScope.ServiceProvider.GetRequiredService<ScopeGenerationWorker>();
                    var db = testScope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var plumbingJobId = Guid.Parse("35b0adba-83f9-4267-800f-4c66d6caa098");
                    
                    sb.AppendLine($"Running ProcessJobAsync for JobPost {plumbingJobId}...");
                    await worker.ProcessJobAsync(plumbingJobId, CancellationToken.None);
                    sb.AppendLine("ProcessJobAsync completed.");

                    // Let's check what was saved
                    var jobPost = db.JobPosts
                        .Include(jp => jp.JobTasks)
                        .ThenInclude(jt => jt.AcceptanceCriteria)
                        .FirstOrDefault(jp => jp.Id == plumbingJobId);

                    sb.AppendLine($"After Layer 1: JobPost Title: {jobPost.Title}");
                    sb.AppendLine($"Status: {jobPost.Status}");
                    sb.AppendLine($"AdminFeedback: {jobPost.AdminFeedback}");
                    sb.AppendLine($"GeneratedScope Length: {jobPost.GeneratedScope?.Length ?? 0}");
                    sb.AppendLine($"Tasks Count: {jobPost.JobTasks.Count}");
                    foreach (var t in jobPost.JobTasks)
                    {
                        sb.AppendLine($"  - Task: {t.Title} (Criteria count: {t.AcceptanceCriteria.Count})");
                    }

                    // Now run Layer 2 (Pricing)
                    sb.AppendLine($"Running ProcessPricingAsync for JobPost {plumbingJobId}...");
                    await worker.ProcessPricingAsync(plumbingJobId, CancellationToken.None);
                    sb.AppendLine("ProcessPricingAsync completed.");

                    // Let's check calculation results
                    var aiCalc = db.AiCalculations
                        .Include(c => c.Tasks)
                        .ThenInclude(t => t.SkuItems)
                        .FirstOrDefault(c => c.ProjectId == jobPost.ProjectId && c.ServiceCategoryId == jobPost.ServiceCategoryId);

                    if (aiCalc == null)
                    {
                        sb.AppendLine("AiCalculation was NOT created or found!");
                    }
                    else
                    {
                        sb.AppendLine($"AiCalculation Id: {aiCalc.Id}");
                        sb.AppendLine($"Tasks Count: {aiCalc.Tasks.Count}");
                        sb.AppendLine($"Total Estimated Price: {aiCalc.TotalEstimatedPrice}");
                        foreach (var t in aiCalc.Tasks)
                        {
                            sb.AppendLine($"  - Task: '{t.Title}' | Estimated Price: {t.EstimatedPrice} | SKU items: {t.SkuItems.Count}");
                            foreach (var sku in t.SkuItems)
                            {
                                sb.AppendLine($"    * SkuId: {sku.ServiceSkuId} | Quantity: {sku.Quantity} | Price: {sku.EstimatedPrice}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("EXCEPTION RUNNING DIAGNOSTICS:");
                sb.AppendLine(ex.ToString());
            }

            var targetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Diagnostics");
            Directory.CreateDirectory(targetDir);
            File.WriteAllText(Path.Combine(targetDir, "diagnostics_run.txt"), sb.ToString());
        }
    }
}
