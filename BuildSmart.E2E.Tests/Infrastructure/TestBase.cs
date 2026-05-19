using Microsoft.Playwright.NUnit;
using Microsoft.Playwright;
using System.Diagnostics;
using System.Net.Http;

namespace BuildSmart.E2E.Tests.Infrastructure;

public abstract class TestBase : PageTest
{
    protected BuildSmartWebAppFactory Factory { get; private set; } = null!;
    protected string WebUrl { get; private set; } = "http://localhost:59125";
    protected string ApiUrl { get; private set; } = "http://localhost:7213";
    protected string BaseUrl => WebUrl; // To maintain compatibility with existing tests
    
    private Process? _apiProcess;
    private Process? _webProcess;

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        // 1. Create the factory which starts the Testcontainer DB
        Factory = new BuildSmartWebAppFactory();
        await Factory.InitializeDbAsync();

        var connectionString = Factory.DbContainer.GetConnectionString();
        var baseDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..");
        var apiPath = Path.Combine(baseDir, "BuildSmart.Api");
        var webPath = Path.Combine(baseDir, "BuildSmart.Web");
        
        // 2. Start the API
        var apiStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{apiPath}\" --urls \"{ApiUrl}\"",
            EnvironmentVariables = 
            {
                ["ConnectionStrings__DefaultConnection"] = connectionString
            },
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        _apiProcess = Process.Start(apiStartInfo);
        
        // Log API output
        Task.Run(() => 
        {
            using var reader = _apiProcess.StandardOutput;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                TestContext.Progress.WriteLine($"[API] {line}");
            }
        });
        Task.Run(() => 
        {
            using var reader = _apiProcess.StandardError;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                TestContext.Progress.WriteLine($"[API ERR] {line}");
            }
        });

        // Wait for API to boot up
        using var client = new HttpClient();
        var retries = 0;
        while (retries < 20)
        {
            try
            {
                var response = await client.GetAsync($"{ApiUrl}/graphql?query={{__schema{{types{{name}}}}}}"); 
                break;
            }
            catch
            {
                await Task.Delay(1000);
                retries++;
            }
        }

        // 3. Start the Web App
        var webStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{webPath}\" --urls \"{WebUrl}\"",
            EnvironmentVariables = 
            {
                ["ApiConfig__BaseUrlOverride"] = ApiUrl
            },
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        _webProcess = Process.Start(webStartInfo);

        // Log Web output
        Task.Run(() => 
        {
            using var reader = _webProcess.StandardOutput;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                TestContext.Progress.WriteLine($"[WEB] {line}");
            }
        });
        Task.Run(() => 
        {
            using var reader = _webProcess.StandardError;
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                TestContext.Progress.WriteLine($"[WEB ERR] {line}");
            }
        });

        // Wait for Web App to boot up
        retries = 0;
        while (retries < 20)
        {
            try
            {
                var response = await client.GetAsync($"{WebUrl}");
                break;
            }
            catch
            {
                await Task.Delay(1000);
                retries++;
            }
        }
    }

    [OneTimeTearDown]
    public async Task GlobalTeardown()
    {
        if (_apiProcess != null && !_apiProcess.HasExited)
        {
            _apiProcess.Kill(true);
            _apiProcess.Dispose();
        }

        if (_webProcess != null && !_webProcess.HasExited)
        {
            _webProcess.Kill(true);
            _webProcess.Dispose();
        }

        if (Factory != null)
        {
            await Factory.DisposeDbAsync();
            await Factory.DisposeAsync();
        }
    }

    [SetUp]
    public async Task SetupContext()
    {
        // Clear cookies and state between individual tests
        await Context.ClearCookiesAsync();
    }
}
