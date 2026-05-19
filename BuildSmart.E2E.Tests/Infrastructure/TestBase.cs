using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

[assembly: Parallelizable(ParallelScope.Fixtures)]

namespace BuildSmart.E2E.Tests.Infrastructure;

[Parallelizable(ParallelScope.Fixtures)]
public abstract class TestBase : PageTest
{
    protected BuildSmartWebAppFactory Factory { get; private set; } = null!;
    
    // Read the BaseUrl from Docker Compose, fallback to local test server
    protected string BaseUrl => Environment.GetEnvironmentVariable("BaseUrl") ?? "http://localhost:59125";

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        // Initialize and migrate the DB (reads from ConnectionStrings__DefaultConnection)
        Factory = new BuildSmartWebAppFactory();
        await Factory.InitializeDbAsync();
    }

    [OneTimeTearDown]
    public async Task GlobalTeardown()
    {
        if (Factory != null)
        {
            await Factory.DisposeDbAsync();
            await Factory.DisposeAsync();
        }
    }

    [SetUp]
    public async Task SetupContext()
    {
        // Clear cookies and state between individual tests to ensure isolation
        await Context.ClearCookiesAsync();
    }
}
