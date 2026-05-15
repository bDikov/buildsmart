using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.TestHost;
using System.Linq;
using BuildSmart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Api.Tests
{
    public class TestApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                // Remove all Entity Framework Core services to ensure a clean slate for the InMemory provider
                var descriptors = services.Where(
                    d => d.ServiceType.Namespace != null && 
                         d.ServiceType.Namespace.StartsWith("Microsoft.EntityFrameworkCore")).ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });

                // We want to override the default authentication scheme with our test scheme.
                services.AddAuthentication(options => 
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                })
                    .AddScheme<TestAuthHandlerOptions, TestAuthHandler>(TestAuthHandler.SchemeName, options => { });
            });
        }
    }
}
