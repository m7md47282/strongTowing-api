namespace StrongTowing.API.Options;

/// <summary>
/// Builds absolute URLs for branded emails (same assets as <c>fontend/towing-web/public/</c>, e.g. logo.svg).
/// Set <see cref="PublicWebBaseUrl"/> to your deployed SPA origin (no trailing slash).
/// </summary>
public sealed class EmailBrandingOptions
{
    public const string SectionName = "EmailBranding";

    /// <summary>HTTPS (or http for local dev) origin of the Angular app, e.g. https://strongtowing.net — no trailing slash.</summary>
    public string? PublicWebBaseUrl { get; set; }

    /// <summary>Path under the public web root; default matches <c>public/images/logo.svg</c>.</summary>
    public string LogoPath { get; set; } = "/images/logo.svg";

    /// <summary>Resolved absolute URL for the logo image in HTML emails, or null if not configured.</summary>
    public string? ResolveLogoAbsoluteUrl()
    {
        var baseUrl = PublicWebBaseUrl?.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
            return null;

        var path = string.IsNullOrWhiteSpace(LogoPath)
            ? "/images/logo.svg"
            : LogoPath.Trim();
        if (!path.StartsWith('/'))
            path = "/" + path;

        var combined = baseUrl + path;
        return Uri.TryCreate(combined, UriKind.Absolute, out var u) &&
               (u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeHttp)
            ? combined
            : null;
    }
}
