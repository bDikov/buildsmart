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
				// 1. Load HTML Template
				string templatePath = Path.Combine(_templateDirectory, "OfferTemplate.html");
				if (!File.Exists(templatePath))
				{
					// Fallback to project root if running from tests or un-published app
					templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Templates", "OfferTemplate.html");
				}

				if (!File.Exists(templatePath))
				{
					// Final fallback
					templatePath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())?.FullName ?? string.Empty, "BuildSmart.Infrastructure", "Resources", "Templates", "OfferTemplate.html");
				}

				if (!File.Exists(templatePath))
				{
					throw new FileNotFoundException($"Template not found at {templatePath}");
				}

				string templateSource = await File.ReadAllTextAsync(templatePath);

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

				// If running in Docker (Linux), use the system-installed Chromium
				if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
				{
					launchOptions.ExecutablePath = "/usr/bin/chromium";
				}
				else
				{
					_logger.LogInformation("Downloading Chromium for local Windows/macOS PDF Generation...");
					var fetcher = new BrowserFetcher();
					await fetcher.DownloadAsync();
				}

				using var browser = await Puppeteer.LaunchAsync(launchOptions);

				using var page = await browser.NewPageAsync();

				// Wait until all assets (like Tailwind CDN) are loaded
				await page.SetContentAsync(populatedHtml, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle0 } });

				_logger.LogInformation("Printing to PDF...");
				var pdfStream = await page.PdfStreamAsync(new PdfOptions
				{
					Format = PaperFormat.A4,
					PrintBackground = true, // Required for Tailwind background colors and borders
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
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to generate PDF offer.");
				throw;
			}
		}
	}
}