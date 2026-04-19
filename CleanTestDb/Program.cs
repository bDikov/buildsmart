using System;
using System.Linq;
using System.Threading.Tasks;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("../BuildSmart.Api/appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("../BuildSmart.Api/appsettings.Development.json", optional: true, reloadOnChange: true);
        var config = builder.Build();
        var connectionString = config.GetConnectionString("DefaultConnection");
        
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        
        using var context = new AppDbContext(optionsBuilder.Options);
        
        var categories = await context.ServiceCategories
            .Where(c => c.Name.StartsWith("Electrical Test") || c.Name.StartsWith("Electrical Room") || c.Name.StartsWith("Complex Electrical"))
            .ToListAsync();
            
        if(categories.Any()) {
            context.ServiceCategories.RemoveRange(categories);
            await context.SaveChangesAsync();
            Console.WriteLine($"Cleaned up {categories.Count} mock test categories from your real database!");
        }

        var users = await context.Users.Where(u => u.Email.StartsWith("test-ai") || u.Email.StartsWith("test-restore")).ToListAsync();
        if(users.Any()) {
            context.Users.RemoveRange(users);
            await context.SaveChangesAsync();
            Console.WriteLine($"Cleaned up {users.Count} mock test users from your real database!");
        }
        
        var projects = await context.Projects.Where(p => p.Title == "AI Test Project" || p.Title == "Basement Office Conversion" || p.Title == "Living Room Tech Upgrade").ToListAsync();
        if(projects.Any()) {
            context.Projects.RemoveRange(projects);
            await context.SaveChangesAsync();
            Console.WriteLine($"Cleaned up {projects.Count} mock test projects from your real database!");
        }
    }
}