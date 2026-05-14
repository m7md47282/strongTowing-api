using System.Runtime.InteropServices;
using Microsoft.Playwright;

namespace StrongTowing.API.Infrastructure;

/// <summary>
/// At startup: configure browser directory, then ensure Chromium exists (install if needed).
/// </summary>
public sealed class PlaywrightBootstrapHostedService(
    IWebHostEnvironment env,
    ILogger<PlaywrightBootstrapHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (PlaywrightChromiumBootstrap.SkipBrowserDownload)
        {
            logger.LogInformation("PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1 — skipping Chromium bootstrap.");
            return;
        }

        VerifyShippedNodeBinary();

        var browserDir = PlaywrightChromiumBootstrap.ConfigureBrowserDirectory(env);

        IPlaywright? playwright = null;
        try
        {
            playwright = await Playwright.CreateAsync().ConfigureAwait(false);
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = LaunchArgs()
            }).ConfigureAwait(false);
            await browser.CloseAsync().ConfigureAwait(false);
            logger.LogInformation("Playwright Chromium is already available at {Path}.", browserDir);
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Chromium not usable yet; running bundled Playwright installer…");
        }
        finally
        {
            playwright?.Dispose();
        }

        try
        {
            var exitCode = PlaywrightChromiumBootstrap.InstallChromium();
            if (exitCode != 0)
            {
                logger.LogError(
                    "Playwright install chromium exited with code {ExitCode}. Quote PDF will fail until browsers install successfully.",
                    exitCode);
                return;
            }

            logger.LogInformation("Playwright Chromium installed under {Path}.", browserDir);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Playwright browser install failed. On the server, ensure outbound HTTPS is allowed and the app can write to {Path}.",
                browserDir);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static string[] LaunchArgs()
    {
        var args = new List<string>();
        if (OperatingSystem.IsLinux())
        {
            args.Add("--no-sandbox");
            args.Add("--disable-dev-shm-usage");
        }
        return args.ToArray();
    }

    /// <summary>
    /// Sanity check: the published <c>.playwright/node/&lt;rid&gt;/node[.exe]</c> must
    /// match the OS we're actually running on. A wrong-RID publish (e.g. building on
    /// macOS and copying the bundle to Windows IIS) is the most common cause of
    /// <c>Playwright.CreateAsync()</c> failing silently before any PDF is generated.
    /// </summary>
    private void VerifyShippedNodeBinary()
    {
        var nodeRoot = Path.Combine(AppContext.BaseDirectory, ".playwright", "node");
        var expected = OperatingSystem.IsWindows()
            ? "win32_x64"
            : OperatingSystem.IsMacOS()
                ? (RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "darwin-arm64" : "darwin-x64")
                : (RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64");

        if (!Directory.Exists(nodeRoot))
        {
            logger.LogWarning(
                "Playwright driver folder not found at {NodeRoot}. PDF generation will fail until the bundle is republished with the Playwright assets.",
                nodeRoot);
            return;
        }

        var available = Directory.GetDirectories(nodeRoot).Select(Path.GetFileName).ToArray();
        if (!available.Contains(expected, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogError(
                "Playwright RID mismatch. Host needs '{Expected}' but the publish only contains [{Available}]. Republish with `dotnet publish -r {Rid} -p:PlaywrightPlatform=win` (or use the IIS publish profile).",
                expected,
                string.Join(", ", available),
                RuntimeInformation.RuntimeIdentifier);
        }
        else
        {
            logger.LogInformation("Playwright driver OK — found {Expected} under {NodeRoot}.", expected, nodeRoot);
        }
    }
}
