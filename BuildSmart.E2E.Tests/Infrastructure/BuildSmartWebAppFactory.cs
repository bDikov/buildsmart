using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using BuildSmart.Infrastructure.Persistence;

namespace BuildSmart.E2E.Tests.Infrastructure;

public class BuildSmartWebAppFactory : WebApplicationFactory<Program>
{
    public readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("buildsmart_test_db")
        .WithUsername("test_user")
        .WithPassword("test_pass")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing AppDbContext registration
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));

            // Register the DbContext to use the Testcontainer connection string
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(DbContainer.GetConnectionString());
            });
        });
    }

    public async Task InitializeDbAsync()
    {
        // 1. Start the Docker container
        await DbContainer.StartAsync();

        // 2. Apply EF Core Migrations to the new test database
        // We use the factory's internal ServiceProvider to get the DbContext
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeDbAsync()
    {
        await DbContainer.DisposeAsync();
    }
}

