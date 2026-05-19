using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using BuildSmart.Infrastructure.Persistence;
using System;

namespace BuildSmart.E2E.Tests.Infrastructure;

public class BuildSmartWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public BuildSmartWebAppFactory()
    {
        // Use environment variable provided by docker-compose, fallback to local test DB for VS runs
        _connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
            ?? "Host=localhost;Port=5432;Database=buildsmart_test_db;Username=test_user;Password=test_pass";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing AppDbContext registration
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));

            // Register the DbContext to use our connection string
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_connectionString);
            });
        });
    }

    public async Task InitializeDbAsync()
    {
        // Apply EF Core Migrations to the test database
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Ensure the database is created and migrated
        await dbContext.Database.MigrateAsync();
    }

    public Task DisposeDbAsync()
    {
        return Task.CompletedTask;
    }
}


