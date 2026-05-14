using System.Reflection;
using Microsoft.Playwright;

namespace StrongTowing.API.Infrastructure;

/// <summary>Shared Playwright browser path + CLI install (used at startup and on-demand for PDF).</summary>
public static class PlaywrightChromiumBootstrap
{
    /// <summary>
    /// Puts Chromium under <c>{ContentRoot}/.pw-browsers</c> and points Playwright at the published driver folder.
    /// </summary>
    public static string ConfigureBrowserDirectory(IWebHostEnvironment env)
    {
        var browserDir = Path.Combine(env.ContentRootPath, ".pw-browsers");
        Directory.CreateDirectory(browserDir);
        Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", browserDir);
        Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", AppContext.BaseDirectory);
        return browserDir;
    }

    public static bool SkipBrowserDownload =>
        string.Equals(Environment.GetEnvironmentVariable("PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD"), "1",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>Runs <c>playwright install chromium</c> via the bundled CLI (exit code 0 = success).</summary>
    public static int InstallChromium()
    {
        if (SkipBrowserDownload)
            return 0;

        var programType = typeof(Playwright).Assembly.GetType("Microsoft.Playwright.Program")
            ?? throw new InvalidOperationException("Microsoft.Playwright.Program type not found in Playwright assembly.");
        var main = programType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static, null, [typeof(string[])], null)
            ?? throw new InvalidOperationException("Microsoft.Playwright.Program.Main(string[]) not found.");
        var result = main.Invoke(null, [new[] { "install", "chromium" }]);
        return result is int code ? code : 1;
    }
}
