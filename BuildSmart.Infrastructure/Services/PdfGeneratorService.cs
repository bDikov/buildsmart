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

				string localChromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
				string localEdgePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";

				if (OperatingSystem.IsWindows() && File.Exists(localChromePath))
				{
					_logger.LogInformation($"Using local Chrome installation: {localChromePath}");
					launchOptions.ExecutablePath = localChromePath;
				}
				else if (OperatingSystem.IsWindows() && File.Exists(localEdgePath))
				{
					_logger.LogInformation($"Using local Edge installation: {localEdgePath}");
					launchOptions.ExecutablePath = localEdgePath;
				}
				else
				{
					_logger.LogInformation("Downloading Chromium into safe Temp folder...");
					var fetcherOptions = new BrowserFetcherOptions { Path = Path.GetTempPath() };
					var fetcher = new BrowserFetcher(fetcherOptions);
					var installedBrowser = await fetcher.DownloadAsync();
					launchOptions.ExecutablePath = installedBrowser.GetExecutablePath();
				}

				using var browser = await Puppeteer.LaunchAsync(launchOptions);
				using var page = await browser.NewPageAsync();

				// We avoid Networkidle0 because external assets (Tailwind, Fonts) might redirect
				// and cause PuppeteerSharp to throw 'Response body is unavailable for redirect responses'
				// on internal background tasks. We use simple DOMContentLoaded load validation instead.

				try
				{
					await page.SetContentAsync(populatedHtml, new NavigationOptions 
					{ 
						WaitUntil = new[] { WaitUntilNavigation.Load, WaitUntilNavigation.DOMContentLoaded },
						Timeout = 30000
					});

					// Dynamic Javascript to calculate and inject layout spacers for flowing sections to push footers to page bottom
					await page.EvaluateExpressionAsync(@"
						(() => {
							const fixedPage = document.querySelector('.pdf-page-fixed');
							if (!fixedPage) return;
							const pageHeight = fixedPage.offsetHeight;
							if (pageHeight <= 0) return;

							const flowingPages = document.querySelectorAll('.pdf-page-flow');
							flowingPages.forEach(flowPage => {
								const footerBlock = flowPage.querySelector('.pdf-footer-block');
								if (!footerBlock) return;

								// Reset any existing spacer first
								const existingSpacer = flowPage.querySelector('.print-spacer');
								if (existingSpacer) {
									existingSpacer.remove();
								}

								// Get the cumulative height of all preceding siblings inside the flowing container
								let contentHeight = 0;
								for (let child of flowPage.children) {
									if (child === footerBlock || child.classList.contains('print-spacer')) {
										break;
									}
									contentHeight += child.offsetHeight;
								}

								// In print, the A4 page height is pageHeight.
								// Each page has 15mm top (57px) and 10mm bottom (38px) padding (total 25mm = 95px).
								// So the printable content area on each page is exactly pageContentHeight.
								const pageContentHeight = pageHeight - 95;
								const footerHeight = footerBlock.offsetHeight;

								const currentPageOffset = contentHeight % pageContentHeight;
								let spacerHeight = pageContentHeight - currentPageOffset - footerHeight;
								if (spacerHeight < 0) {
									// If it doesn't fit on the current page, push to the next page
									spacerHeight = pageContentHeight + spacerHeight;
								}

								if (spacerHeight > 5) {
									const spacer = document.createElement('div');
									spacer.style.height = spacerHeight + 'px';
									spacer.className = 'print-spacer';
									footerBlock.parentNode.insertBefore(spacer, footerBlock);
								}
							});
						})()
					");
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