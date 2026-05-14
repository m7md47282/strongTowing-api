using Microsoft.Playwright;
using StrongTowing.API.Infrastructure;
using StrongTowing.Application.DTOs.Requests;

namespace StrongTowing.API.Services;

public sealed class PlaywrightQuotePdfGenerator(
    IWebHostEnvironment webEnv,
    IHostEnvironment env,
    ILogger<PlaywrightQuotePdfGenerator> logger) : IQuotePdfGenerator
{
    public async Task<byte[]> GenerateAsync(QuotePdfRequest request, string assetBaseUrl,
        CancellationToken cancellationToken = default)
    {
        if (!PlaywrightChromiumBootstrap.SkipBrowserDownload)
            PlaywrightChromiumBootstrap.ConfigureBrowserDirectory(webEnv);

        try
        {
            return await GeneratePdfOnceAsync(request, assetBaseUrl, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested &&
                                     !PlaywrightChromiumBootstrap.SkipBrowserDownload)
        {
            logger.LogWarning(ex, "Quote PDF first attempt failed for {QuoteRef}; running install chromium and retrying once.", request.QuoteRef);

            var code = PlaywrightChromiumBootstrap.InstallChromium();
            if (code != 0)
                throw new InvalidOperationException($"playwright install chromium exited with code {code}.", ex);

            return await GeneratePdfOnceAsync(request, assetBaseUrl, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<byte[]> GeneratePdfOnceAsync(QuotePdfRequest request, string assetBaseUrl,
        CancellationToken cancellationToken)
    {
        // Pass the WebRoot path so the builder can inline /wwwroot/quote-assets/* as data: URIs.
        // This avoids depending on server loopback DNS, NAT hairpinning, self-signed certs, or
        // the API-to-itself round trip — all common reasons SVG/JPG assets render as blank
        // boxes inside the headless Chromium PDF.
        var html = QuotePdfHtmlBuilder.Build(request, assetBaseUrl, webEnv.WebRootPath);
        IPlaywright? playwright = null;
        IBrowser? browser = null;
        try
        {
            playwright = await Playwright.CreateAsync().ConfigureAwait(false);
            var args = new List<string>();
            if (env.IsDevelopment())
                args.Add("--ignore-certificate-errors");
            if (OperatingSystem.IsLinux() || ForceNoSandbox())
            {
                args.Add("--no-sandbox");
                args.Add("--disable-dev-shm-usage");
            }

            var launch = new BrowserTypeLaunchOptions { Headless = true };
            if (args.Count > 0)
                launch.Args = args;

            browser = await playwright.Chromium.LaunchAsync(launch).ConfigureAwait(false);
            var page = await browser.NewPageAsync().ConfigureAwait(false);

            // NetworkIdle waits for all in-flight requests (Font Awesome CDN + any
            // remaining external assets) to settle. DOMContentLoaded fires too early
            // and was leaving the PDF without icons / payment marks.
            await page.SetContentAsync(html, new PageSetContentOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60_000
            }).ConfigureAwait(false);

            // Belt-and-suspenders: explicitly wait for webfonts (Font Awesome glyphs)
            // before snapshotting, then a tiny paint tick.
            await page.EvaluateAsync(
                "async () => { if (document.fonts && document.fonts.ready) { await document.fonts.ready; } }"
            ).ConfigureAwait(false);
            await Task.Delay(200, cancellationToken).ConfigureAwait(false);

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

    private static bool ForceNoSandbox() =>
        string.Equals(Environment.GetEnvironmentVariable("PLAYWRIGHT_FORCE_NO_SANDBOX"), "1",
            StringComparison.OrdinalIgnoreCase);
}
