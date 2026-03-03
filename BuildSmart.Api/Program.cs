using HotChocolate.AspNetCore;
using BuildSmart.Api.GraphQL;
using BuildSmart.Api.GraphQL.Types;
using BuildSmart.Api.Workers;
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
using Microsoft.AspNetCore.Authentication.Google;
using AspNet.Security.OAuth.Apple;

[assembly: InternalsVisibleTo("BuildSmart.Api.Tests")]

public partial class Program
{
	public static async Task Main(string[] args)
	{
		var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

		var builder = WebApplication.CreateBuilder(args);

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
		builder.Services.AddScoped<IUserRepository, UserRepository>();
		builder.Services.AddScoped<ITradesmanProfileRepository, TradesmanProfileRepository>();
		builder.Services.AddScoped<IBookingRepository, BookingRepository>();
		builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
		builder.Services.AddScoped<IServiceCategoryRepository, ServiceCategoryRepository>();
		builder.Services.AddScoped<IProjectRepository, ProjectRepository>(); // Added ProjectRepository registration
		builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

		// Add Application Services (Business Logic)
		builder.Services.AddScoped<IBookingService, BookingService>();
		builder.Services.AddScoped<ITradesmanProfileService, TradesmanProfileService>();
		builder.Services.AddScoped<IReviewService, ReviewService>();
		builder.Services.AddScoped<IJobPostService, JobPostService>();
		builder.Services.AddScoped<IJobsNotificationService, BuildSmart.Api.Services.JobsNotificationService>();
		builder.Services.AddScoped<DataMigrationService>();
		builder.Services.AddScoped<IAuthService, AuthService>();
		builder.Services.AddScoped<INotificationService, BuildSmart.Api.Services.NotificationService>();
		builder.Services.AddScoped<IMultimediaStorageService, BuildSmart.Infrastructure.Services.LocalMultimediaStorageService>();

		// --- Background Services (Scope Generation) ---
		builder.Services.AddSingleton<IScopeGenerationQueue, ScopeGenerationQueue>();
		builder.Services.AddHostedService<ScopeGenerationWorker>();
		builder.Services.AddScoped<IAiService, MockAiService>();

		// --- JWT Authentication Setup ---
		builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
			options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
			options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
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

		builder.Services.AddControllers();
		builder.Services.AddSignalR();
		builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>(); // Added CustomUserIdProvider

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
			.AddUploadType()
	.AddQueryType<QueryType>()
	.AddMutationType<MutationType>()
	.AddType<BuildSmart.Api.GraphQL.Types.TradesmanProfileType>()
			.AddType<TradesmanSkillType>() 
			.AddType<UserType>()
			.AddType<JobPostType>()
			.AddType<BookingType>()
			.AddType<ReviewType>()
			.AddType<CertificationType>()
			.AddType<PortfolioEntryType>()
			.AddType<JobPostQuestionType>()
            .AddType<JobPostFeedbackType>()
			.AddProjections()
			.AddFiltering()
			.AddSorting()
			.AddAuthorization()
			// FIX 2: Use the service provider to get the connection string when needed.
			.AddPostgresSubscriptions(options =>
			{
				var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

				options.ConnectionFactory = (token) =>
				{
					return new ValueTask<NpgsqlConnection>(new NpgsqlConnection(connectionString));
				};
			});

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
				context.Database.Migrate(); // Apply any pending migrations
				await context.SeedAdminUser(); // Seed the admin user
				await context.SeedHomeownerUser(); // Seed the homeowner user
				await context.SeedTradesmanUser(); // Seed the painter tradesman
			}
			catch (Exception ex)
			{
				var logger = services.GetRequiredService<ILogger<Program>>();
				logger.LogError(ex, "An error occurred while migrating or seeding the database.");
			}
		}

		// --- 2. Configure the HTTP request pipeline ---
		if (app.Environment.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
			app.UseSwagger();
			app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "BuildSmart.Api v1"));
		}

		app.UseCors(MyAllowSpecificOrigins);

		app.UseRouting();

		// Authenticate and Authorize for ALL requests BEFORE any endpoint routing
		app.UseAuthentication();
		app.UseAuthorization();

		// Removed app.UseWhen and BasicAuthMiddleware for /graphql

		// This is the endpoint that our MAUI and Blazor apps will call
		app.MapGraphQL("/graphql");
		app.MapHub<NotificationHub>("/hubs/notifications"); // Added Hub mapping

		app.MapControllers();

		app.Run();
	}
}
