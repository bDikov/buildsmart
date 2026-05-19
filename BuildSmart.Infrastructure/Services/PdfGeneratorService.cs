using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using BuildSmart.Core.Application.Interfaces;
using HandlebarsDotNet;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace BuildSmart.Infrastructure.Services
{
	public class PdfGeneratorService : IPdfGeneratorService
	{
		private readonly ILogger<PdfGeneratorService> _logger;
		private readonly string _templateDirectory;

		public PdfGeneratorService(ILogger<PdfGeneratorService> logger)
		{
			_logger = logger;
			_templateDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "Resources", "Templates");
		}

		public async Task<byte[]> GenerateOfferPdfAsync(object offerData)
		{
			try
			{
				// 1. Load HTML Template (Embedded Resource)
				var assembly = Assembly.GetExecutingAssembly();
				var resourceName = "BuildSmart.Infrastructure.Resources.Templates.OfferTemplate.html";
				
				using var stream = assembly.GetManifestResourceStream(resourceName);
				if (stream == null)
				{
					var availableResources = string.Join(", ", assembly.GetManifestResourceNames());
					throw new FileNotFoundException($"Embedded resource not found: {resourceName}. Available: {availableResources}");
				}
				using StreamReader reader = new StreamReader(stream);
				string templateSource = await reader.ReadToEndAsync();

				// 2. Bind Data with Handlebars
				var template = Handlebars.Compile(templateSource);
				string populatedHtml = template(offerData);

				// 3. Setup PuppeteerSharp
				_logger.LogInformation("Launching Headless Chrome...");
				
				var launchOptions = new LaunchOptions
				{
					Headless = true,
					Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage", "--disable-gpu" }
				};

				_logger.LogInformation("Downloading Chromium into safe Temp folder...");
				var fetcherOptions = new BrowserFetcherOptions { Path = Path.GetTempPath() };
				var fetcher = new BrowserFetcher(fetcherOptions);
				var installedBrowser = await fetcher.DownloadAsync();
				launchOptions.ExecutablePath = installedBrowser.GetExecutablePath();

				using var browser = await Puppeteer.LaunchAsync(launchOptions);
				using var page = await browser.NewPageAsync();

				// We avoid Networkidle0 because external assets (Tailwind, Fonts) might redirect
				// and cause PuppeteerSharp to throw 'Response body is unavailable for redirect responses'
				// on internal background tasks.
				try
				{
					await page.SetContentAsync(populatedHtml, new NavigationOptions 
					{ 
						WaitUntil = new[] { WaitUntilNavigation.Load }, 
						Timeout = 30000 
					});
				}
				catch (Exception contentEx)
				{
					_logger.LogWarning(contentEx, "SetContentAsync finished with warnings. Proceeding to PDF generation anyway.");
				}

				_logger.LogInformation("Printing to PDF...");
				try
				{
					var pdfStream = await page.PdfStreamAsync(new PdfOptions
					{
						Format = PaperFormat.A4,
						PrintBackground = true,
						MarginOptions = new MarginOptions
						{
							Top = "20px",
							Bottom = "20px",
							Left = "20px",
							Right = "20px"
						}
					});

					using var memoryStream = new MemoryStream();
					await pdfStream.CopyToAsync(memoryStream);
					return memoryStream.ToArray();
				}
				catch (Exception pdfEx)
				{
					_logger.LogError(pdfEx, "Failed during PdfStreamAsync phase.");
					throw;
				}
				finally
				{
					// Explicitly close page to avoid leaking tasks
					await page.CloseAsync();
					await browser.CloseAsync();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to generate PDF offer.");
				throw;
			}
		}
	}
}