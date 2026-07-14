using BuildSmart.Api.GraphQL;
using BuildSmart.Api.GraphQL.Types;
using BuildSmart.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Application.Services;
using BuildSmart.Infrastructure.Persistence;
using BuildSmart.Infrastructure.Persistence.Repositories; // Required for accessing IConfiguration
using BuildSmart.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.Text;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Hangfire;
using Hangfire.PostgreSql;
using Serilog;
using Sentry.Hangfire;

[assembly: InternalsVisibleTo("BuildSmart.Api.Tests")]

public partial class Program
{
	public static async Task Main(string[] args)
	{
		// --- Global Error Handling for Background Tasks ---
		TaskScheduler.UnobservedTaskException += (sender, e) =>
		{
			// We handle this so it doesn't crash the process or get reported as a Fatal error.
			// Internal Puppeteer tasks often trigger this due to redirects or network timeouts.
			Log.Warning(e.Exception, "Unobserved Task Exception caught: {Message}", e.Exception.Message);
			e.SetObserved();
		};

		var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

		var builder = WebApplication.CreateBuilder(args);

		// --- Sentry & Serilog Configuration ---
		// We explicitly read SENTRY_DSN from environment variables (set in GitHub Secrets/Docker)
		var sentryDsn = builder.Configuration["SENTRY_DSN"];

		if (!string.IsNullOrWhiteSpace(sentryDsn))
		{
			builder.WebHost.UseSentry(o =>
			{
				o.Dsn = sentryDsn;
				o.Debug = true; // Helpful for initial setup verification
				o.TracesSampleRate = 1.0;
				o.EnableLogs = true; // Enable Sentry logging
				o.SetBeforeSend((@event, hint) =>
				{
					if (@event.Exception != null)
					{
						var exType = @event.Exception.GetType().FullName;
						var exMessage = @event.Exception.Message;

						if (exType != null && exType.Contains("Puppeteer") && exMessage != null && exMessage.Contains("Response body is unavailable"))
						{
							return null; // Don't report to Sentry
						}

						if (@event.Exception is AggregateException aggEx)
						{
							foreach (var inner in aggEx.InnerExceptions)
							{
								var innerType = inner.GetType().FullName;
								var innerMsg = inner.Message;
								if (innerType != null && innerType.Contains("Puppeteer") && innerMsg != null && innerMsg.Contains("Response body is unavailable"))
								{
									return null; // Don't report
								}
							}
						}
					}
					return @event;
				});
			});
		}

		var loggerConfig = new LoggerConfiguration()
			.ReadFrom.Configuration(builder.Configuration)
			.Enrich.FromLogContext()
			.WriteTo.Console();

		if (!string.IsNullOrWhiteSpace(sentryDsn))
		{
			loggerConfig.WriteTo.Sentry(o =>
			{
				o.Dsn = sentryDsn;
			});
		}

		Log.Logger = loggerConfig.CreateLogger();

		builder.Host.UseSerilog();

		builder.Services.AddCors(options =>
		{
			options.AddPolicy(name: MyAllowSpecificOrigins,
							  policy =>
							  {
								  policy.AllowAnyOrigin()
										.AllowAnyHeader()
										.AllowAnyMethod();
							  });
		});

		// --- 1. Add services to the container (Dependency Injection) ---
		builder.Services.AddLocalization();
		builder.Services.AddSingleton<BuildSmart.Core.Application.Interfaces.ILocalizationCacheService, BuildSmart.SharedUI.Services.Localization.LocalizationCacheService>();
		builder.Services.AddSingleton<Microsoft.Extensions.Localization.IStringLocalizerFactory, BuildSmart.SharedUI.Services.Localization.DbStringLocalizerFactory>();

		// Add DbContext and PostgreSQL Connection
		builder.Services.AddDbContext<AppDbContext>(options =>
		{
			options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
						b =>
						{
							b.MigrationsAssembly("BuildSmart.Infrastructure");
							b.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
						});

			// Explicitly suppress warnings that are being treated as errors in this environment
			options.ConfigureWarnings(w => w.Ignore(
				Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning,
				Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.NavigationBaseIncludeIgnored));
		});
		// Add Repositories and UnitOfWork
		builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
		builder.Services.AddScoped<IQuestionRepository, BuildSmart.Infrastructure.Repositories.QuestionRepository>();
		builder.Services.AddScoped<IFormulaRepository, BuildSmart.Infrastructure.Repositories.FormulaRepository>();
		builder.Services.AddScoped<IUserRepository, UserRepository>();
		builder.Services.AddScoped<ITradesmanProfileRepository, TradesmanProfileRepository>();
		builder.Services.AddScoped<IBookingRepository, BookingRepository>();
		builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
		builder.Services.AddScoped<IServiceCategoryRepository, ServiceCategoryRepository>();
		builder.Services.AddScoped<IProjectRepository, ProjectRepository>(); // Added ProjectRepository registration
		builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
		builder.Services.AddScoped<IJobPostQuestionRepository, JobPostQuestionRepository>();
		builder.Services.AddScoped<IJobPostRepository, JobPostRepository>();
		builder.Services.AddScoped<IJobPostFeedbackRepository, JobPostFeedbackRepository>();
		builder.Services.AddScoped<IAuctionActionRepository, AuctionActionRepository>();
		builder.Services.AddScoped<IBidRepository, BidRepository>();
		builder.Services.AddScoped<IProjectMessageRepository, BuildSmart.Infrastructure.Repositories.ProjectMessageRepository>();

		// Add Application Services (Business Logic)
		builder.Services.AddScoped<IServiceCategoryOrchestrator, BuildSmart.Core.Application.Services.ServiceCategoryOrchestrator>();
		builder.Services.AddScoped<IBookingService, BookingService>();
		builder.Services.AddScoped<ITradesmanProfileService, TradesmanProfileService>();
		builder.Services.AddScoped<IReviewService, ReviewService>();
		builder.Services.AddScoped<IJobPostService, JobPostService>();
		builder.Services.AddScoped<IQuestionManagementService, BuildSmart.Core.Application.Services.QuestionManagementService>();
		builder.Services.AddScoped<IPaymentService, PaymentService>();
		builder.Services.AddScoped<IJobsNotificationService, BuildSmart.Api.Services.JobsNotificationService>();
		builder.Services.AddScoped<DataMigrationService>();
		builder.Services.AddScoped<IAuthService, AuthService>();
		builder.Services.AddScoped<INotificationService, BuildSmart.Api.Services.NotificationService>();
		builder.Services.AddScoped<IProjectChatService, ProjectChatService>();
		builder.Services.AddScoped<IMultimediaStorageService, BuildSmart.Infrastructure.Services.LocalMultimediaStorageService>();
		builder.Services.AddScoped<IMediaService, BuildSmart.Infrastructure.Services.CloudflareR2MediaService>();
		builder.Services.AddScoped<IPdfGeneratorService, PdfGeneratorService>();
		builder.Services.AddScoped<IPricingEngine, PricingEngine>();
		builder.Services.AddScoped<IEmailService, EmailService>();
		builder.Services.AddSingleton<IActiveProjectChatTracker, ActiveProjectChatTracker>();
		builder.Services.AddSingleton<IUserPresenceService, UserPresenceService>();

		// --- Background Services (Scope Generation) ---
		builder.Services.AddSingleton<IScopeGenerationQueue, BuildSmart.Api.Services.HangfireScopeGenerationQueue>();
		builder.Services.AddScoped<IAiService, GeminiAiService>();
		builder.Services.AddScoped<BuildSmart.Api.Workers.GuestCleanupJob>();


		// --- Hangfire Configuration ---
		builder.Services.AddHangfire(configuration =>
		{
			configuration
				.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
				.UseSimpleAssemblyNameTypeSerializer()
				.UseRecommendedSerializerSettings()
				.UseSentry(); // Integrates Sentry with Hangfire

			var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
			if (!string.IsNullOrEmpty(connectionString))
			{
				configuration.UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString));
			}
			else
			{
				configuration.UseInMemoryStorage();
			}
		});

		builder.Services.AddHangfireServer(options =>
		{
			options.ServerName = String.Format("{0}:DefaultServer", Environment.MachineName);
			options.Queues = new[] { "default" };
			options.WorkerCount = 2; // Prevent CPU/RAM exhaustion during heavy jobs (like FFmpeg transcoding)
		});

		builder.Services.AddHangfireServer(options =>
		{
			options.ServerName = String.Format("{0}:AiServer", Environment.MachineName);
			options.Queues = new[] { "ai-queue" };
			options.WorkerCount = 1;
		});

		// --- JWT Authentication Setup ---
		builder.Services.AddAuthentication(options =>
		{
			options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
			options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			options.DefaultSignInScheme = "ExternalCookie";
		})
		.AddCookie("ExternalCookie", options =>
		{
			options.Cookie.Name = "ExternalCookie";
			options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
			options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
			options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
		})
		.AddJwtBearer(options =>
		{
			options.TokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuer = true,
				ValidateAudience = true,
				ValidateLifetime = true,
				ValidateIssuerSigningKey = true,
				ValidIssuer = builder.Configuration["Jwt:Issuer"]!,
				ValidAudience = builder.Configuration["Jwt:Audience"]!,
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
				RoleClaimType = ClaimTypes.Role // Explicitly set the role claim type
			};
		})
		.AddGoogle(options =>
		{
			options.SignInScheme = "ExternalCookie";
			options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "YOUR_CLIENT_ID";
			options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "YOUR_CLIENT_SECRET";
			options.CallbackPath = "/api/externalauth/signin-google";
			options.ClaimActions.MapJsonKey("picture", "picture", "url");
			options.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
			options.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
			options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
			{
				OnRemoteFailure = context =>
				{
					var returnUrl = "buildsmart://auth";
					if (context.Properties?.RedirectUri != null)
					{
						try
						{
							var uri = new Uri(context.Properties.RedirectUri);
							var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
							if (query.TryGetValue("returnUrl", out var rUrl))
							{
								returnUrl = rUrl.ToString();
							}
						}
						catch { }
					}
					var errorMessage = context.Failure?.Message ?? "Remote login failed";
					var separator = returnUrl.Contains("?") ? "&" : "?";
					context.Response.Redirect($"{returnUrl}{separator}error={System.Web.HttpUtility.UrlEncode(errorMessage)}");
					context.HandleResponse();
					return Task.CompletedTask;
				}
			};
		});
		// .AddApple(options =>
		// {
		//     options.ClientId = builder.Configuration["Authentication:Apple:ClientId"];
		//     options.KeyId = builder.Configuration["Authentication:Apple:KeyId"];
		//     options.TeamId = builder.Configuration["Authentication:Apple:TeamId"];
		//     options.PrivateKey = (keyId, _) => Task.FromResult<ReadOnlyMemory<char>>(builder.Configuration["Authentication:Apple:PrivateKey"].AsMemory());
		// });

		builder.Services.AddAuthorization(options =>
		{
			options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
		});

		// Configure Forwarded Headers for reverse proxy (Caddy/Docker)
		builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
		{
			options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All;
			options.ForwardLimit = null; // Important: Allow any number of proxies
										 // Clear known networks/proxies to trust all proxies (typical in Docker setups where the proxy IP varies)
			options.KnownNetworks.Clear();
			options.KnownProxies.Clear();
		});

		builder.Services.AddControllers();
		builder.Services.AddSignalR();
		builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>(); // Added CustomUserIdProvider

		builder.Services.AddEndpointsApiExplorer();

		// Add Swagger Services
		builder.Services.AddSwaggerGen(c =>
		{
			c.SwaggerDoc("v1", new OpenApiInfo { Title = "BuildSmart.Api", Version = "v1" });

			// Define the BearerAuth security scheme
			c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
			{
				Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
				Name = "Authorization",
				In = ParameterLocation.Header,
				Type = SecuritySchemeType.Http,
				Scheme = "bearer"
			});

			c.AddSecurityRequirement(new OpenApiSecurityRequirement()
			{
				{
					new OpenApiSecurityScheme
					{
						Reference = new OpenApiReference
						{
							Type = ReferenceType.SecurityScheme,
							Id = "Bearer"
						}
					},
					new string[] { }
				}
			});
		});

		// Add GraphQL Services (Hot Chocolate)
		builder.Services
			.AddGraphQLServer()
			.ModifyCostOptions(o =>
			{
				o.EnforceCostLimits = false;
				o.MaxFieldCost = 10000;
				o.MaxTypeCost = 10000;
			})
			.AddUploadType()
	.AddQueryType<QueryType>()
	.AddMutationType<MutationType>()
	.AddType<BuildSmart.Api.GraphQL.Types.TradesmanProfileType>()
			.AddType<TradesmanSkillType>()
			.AddType<UserType>()
			.AddType<ServiceCategoryType>()
			.AddType<ServiceSkuType>()
			.AddType<JobPostType>()
			.AddType<BookingType>()
			.AddType<MilestonePaymentType>()
			.AddType<ReviewType>()
			.AddType<BidType>()
			.AddType<CertificationType>()
			.AddType<PortfolioEntryType>()
			.AddType<TradesmanMediaType>()
			.AddType<ProjectMilestoneMediaType>()
			.AddType<JobPostQuestionType>()
			.AddType<JobPostFeedbackType>()
			.AddType<JobTaskType>()
			.AddType<TaskAcceptanceCriteriaType>()
			.AddType<TaskSkuItemType>()
			.AddType<BidItemType>()
			.AddType<BuildSmart.Api.GraphQL.Types.Input.SubmitBidInputType>()
			.AddType<BuildSmart.Api.GraphQL.Types.Input.UpdateJobTasksInputType>()
			.AddProjections()
			.AddFiltering()
			.AddSorting()
			.AddAuthorization();

		var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
		if (!string.IsNullOrEmpty(connectionString))
		{
			builder.Services.AddGraphQLServer().AddPostgresSubscriptions(options =>
			{
				options.ConnectionFactory = (token) =>
				{
					return new ValueTask<NpgsqlConnection>(new NpgsqlConnection(connectionString));
				};
			});
		}
		else
		{
			builder.Services.AddGraphQLServer().AddInMemorySubscriptions();
		}

		// Add other services like CORS, etc.
		builder.Services.AddHttpContextAccessor();

		var app = builder.Build();

		// Apply migrations and seed data
		using (var scope = app.Services.CreateScope())
		{
			var services = scope.ServiceProvider;
			try
			{
				var context = services.GetRequiredService<AppDbContext>();
				if (context.Database.IsRelational())
				{
					context.Database.Migrate(); // Apply any pending migrations
				}

				if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.PostgreSQL" ||
					context.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
				{
					using (var transaction = await context.Database.BeginTransactionAsync())
					{
						// Acquire a transaction-level advisory lock. 748291 is our arbitrary key.
						await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(748291);");

						await context.CleanupAndMergeCategoriesAsync(); // Auto-heal suffix duplicate categories
						context.ChangeTracker.Clear();

						// Always run SeedCategoriesAndQuestionsAsync to keep category types synchronized
						await context.SeedCategoriesAndQuestionsAsync();
						context.ChangeTracker.Clear();

						if (!await context.ServiceCategories.AnyAsync())
						{
							Console.WriteLine("Database is empty. Seeding initial categories, SKUs, and default users...");
							await context.SeedSkusAsync(); // Seed the SKUs from JSON data
							context.ChangeTracker.Clear();
							await context.SeedQuestionsAndFormulasAsync(); // Seed relational questions/formulas
							context.ChangeTracker.Clear();
							await context.SeedAdminUser(); // Seed the admin user
							context.ChangeTracker.Clear();
							await context.SeedHomeownerUser(); // Seed the homeowner user
							context.ChangeTracker.Clear();
							await context.SeedTradesmanUser(); // Seed the painter tradesman
							context.ChangeTracker.Clear();
						}
						else
						{
							Console.WriteLine("Database already initialized. Initial seeders skipped (except categories type sync).");
						}

						await transaction.CommitAsync();
					}
				}
				else
				{
					await context.CleanupAndMergeCategoriesAsync(); // Auto-heal suffix duplicate categories
					context.ChangeTracker.Clear();

					// Always run SeedCategoriesAndQuestionsAsync to keep category types synchronized
					await context.SeedCategoriesAndQuestionsAsync();
					context.ChangeTracker.Clear();

					if (!await context.ServiceCategories.AnyAsync())
					{
						Console.WriteLine("Database is empty. Seeding initial categories, SKUs, and default users...");
						await context.SeedSkusAsync(); // Seed the SKUs from JSON data
						context.ChangeTracker.Clear();
						await context.SeedQuestionsAndFormulasAsync(); // Seed relational questions/formulas
						context.ChangeTracker.Clear();
						await context.SeedAdminUser(); // Seed the admin user
						context.ChangeTracker.Clear();
						await context.SeedHomeownerUser(); // Seed the homeowner user
						context.ChangeTracker.Clear();
						await context.SeedTradesmanUser(); // Seed the painter tradesman
						context.ChangeTracker.Clear();
					}
					else
					{
						Console.WriteLine("Database already initialized. Initial seeders skipped (except categories type sync).");
					}
				}

				// Seed localization resources if empty using compiled assembly resources
				var resourceManager = new System.Resources.ResourceManager(
					"BuildSmart.SharedUI.Resources.AppResources",
					typeof(BuildSmart.SharedUI.Resources.AppResources).Assembly
				);
				await context.SeedLocalizationResourcesAsync(resourceManager);

				// Warm up localization cache
				var cacheService = services.GetRequiredService<BuildSmart.Core.Application.Interfaces.ILocalizationCacheService>();
				var resources = await context.LocalizationResources.AsNoTracking().ToListAsync();
				var cacheData = resources
					.GroupBy(r => r.Culture)
					.ToDictionary(
						g => g.Key,
						g => g.GroupBy(r => r.Key, StringComparer.OrdinalIgnoreCase)
							.ToDictionary(k => k.Key, k => k.First().Value, StringComparer.OrdinalIgnoreCase),
						StringComparer.OrdinalIgnoreCase
					);
				cacheService.Initialize(cacheData);
			}
			catch (Exception ex)
			{
				var logger = services.GetRequiredService<ILogger<Program>>();
				logger.LogError(ex, "An error occurred while migrating or seeding the database.");
			}
		}

		// --- 2. Configure the HTTP request pipeline ---
		app.UseForwardedHeaders();

		if (app.Environment.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
		}

		// Enable Swagger in all environments for access via Caddy proxy
		app.UseSwagger();
		app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BuildSmart.Api v1"));

		// Enable serving static files from wwwroot (like the generated PDFs)
		app.UseStaticFiles(new StaticFileOptions
		{
			OnPrepareResponse = ctx =>
			{
				ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
				ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers", "*");
				ctx.Context.Response.Headers.Append("Access-Control-Allow-Methods", "*");
			}
		});

		app.UseCors(MyAllowSpecificOrigins);

		app.UseRouting();

		app.UseMiddleware<BuildSmart.Api.Middleware.LanguageMiddleware>();

		var dashboardToken = builder.Configuration["HANGFIRE_DASHBOARD_TOKEN"];
		app.UseHangfireDashboard("/hangfire", new DashboardOptions
		{
			Authorization = new[] { new BuildSmart.Api.Services.HangfireDashboardAuthorizationFilter(dashboardToken ?? string.Empty) }
		});

		// Register Guest Session Cleanup recurring job
		RecurringJob.AddOrUpdate<BuildSmart.Api.Workers.GuestCleanupJob>(
			"expired-guest-cleanup",
			job => job.RunCleanupAsync(System.Threading.CancellationToken.None),
			Cron.Daily);


		// Authenticate and Authorize for ALL requests BEFORE any endpoint routing
		app.UseAuthentication();
		app.UseAuthorization();

		// Removed app.UseWhen and BasicAuthMiddleware for /graphql

		// This is the endpoint that our MAUI and Blazor apps will call
		app.MapGraphQL("/graphql");
		app.MapHub<BuildSmart.Api.Hubs.NotificationHub>("/hubs/notifications"); // Added Hub mapping
		app.MapHub<BuildSmart.Api.Hubs.JobProcessingHub>("/jobProcessingHub");

		app.MapControllers();

		app.Run();
	}
}