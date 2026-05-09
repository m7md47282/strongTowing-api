using Microsoft.Playwright;
using StrongTowing.Application.DTOs.Requests;

namespace StrongTowing.API.Services;

public sealed class PlaywrightQuotePdfGenerator(
    IHostEnvironment env,
    ILogger<PlaywrightQuotePdfGenerator> logger) : IQuotePdfGenerator
{
    public async Task<byte[]> GenerateAsync(QuotePdfRequest request, string assetBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var html = QuotePdfHtmlBuilder.Build(request, assetBaseUrl);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            playwright = await Playwright.CreateAsync().ConfigureAwait(false);
            var launch = new BrowserTypeLaunchOptions { Headless = true };
            if (env.IsDevelopment())
                launch.Args = ["--ignore-certificate-errors"];

            browser = await playwright.Chromium.LaunchAsync(launch).ConfigureAwait(false);
            var page = await browser.NewPageAsync().ConfigureAwait(false);
            await page.SetContentAsync(html, new PageSetContentOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 120_000
            }).ConfigureAwait(false);

            await Task.Delay(800, cancellationToken).ConfigureAwait(false);

            return await page.PdfAsync(new PagePdfOptions
            {
                PrintBackground = true,
                Format = PaperFormat.Letter,
                Margin = new Margin { Top = "10mm", Bottom = "10mm", Left = "10mm", Right = "10mm" }
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Playwright PDF generation failed for quote {QuoteRef}", request.QuoteRef);
            throw;
        }
        finally
        {
            if (browser != null)
                await browser.DisposeAsync().ConfigureAwait(false);
            playwright?.Dispose();
        }
    }
}
