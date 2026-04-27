using System.Net;
using System.Text.RegularExpressions;

namespace StrongTowing.API.Services;

public static class EmailLayout
{
    public const string DefaultCompanyName = "Strong Towing";

    public static string WrapInFormalLayout(string? logoUrlAbsolute, string mergedInnerHtml)
    {
        var header = !string.IsNullOrWhiteSpace(logoUrlAbsolute) &&
            Uri.TryCreate(logoUrlAbsolute!.Trim(), UriKind.Absolute, out var u) &&
            (string.Equals(u.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(u.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            ? $"""<img src="{WebUtility.HtmlEncode(logoUrlAbsolute!.Trim())}" alt="{WebUtility.HtmlEncode(DefaultCompanyName)}" width="200" style="max-width:200px;height:auto;border:0;display:inline-block;vertical-align:middle;"/>"""
            : $"""<span style="font-size:20px;font-weight:600;color:#0f172a;letter-spacing:-0.02em;font-family:system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;">{WebUtility.HtmlEncode(DefaultCompanyName)}</span>""";

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="utf-8"/><meta name="viewport" content="width=device-width, initial-scale=1.0"/></head>
            <body style="margin:0;padding:0;background-color:#f1f5f9;">
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f1f5f9;padding:32px 16px;">
            <tr><td align="center">
            <table role="presentation" width="600" cellspacing="0" cellpadding="0" style="max-width:600px;width:100%;background-color:#ffffff;border:1px solid #e2e8f0;border-radius:8px;overflow:hidden;">
            <tr><td style="padding:28px 32px 20px 32px;text-align:center;border-bottom:1px solid #e2e8f0;background:linear-gradient(to bottom,#ffffff,#f8fafc);">
            {header}
            </td></tr>
            <tr><td style="padding:32px 32px 28px 32px;font-family:Georgia,'Times New Roman',serif;font-size:16px;line-height:1.65;color:#0f172a;">
            {mergedInnerHtml}
            </td></tr>
            <tr><td style="padding:20px 32px;border-top:1px solid #e2e8f0;font-family:system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;font-size:12px;line-height:1.5;color:#64748b;text-align:center;">
            {WebUtility.HtmlEncode(DefaultCompanyName)} · This is an automated message. Please do not reply directly to this email.
            </td></tr>
            </table>
            </td></tr>
            </table>
            </body>
            </html>
            """;
    }

    /// <summary>Derive a readable plain-text body from the final HTML when no custom text is stored.</summary>
    public static string HtmlToPlainText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var t = html;
        t = Regex.Replace(t, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, @"</(p|div|h[1-6]|tr)>", "\n", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, "<[^>]+>", string.Empty);
        t = WebUtility.HtmlDecode(t);
        t = Regex.Replace(t, "\r\n|\r", "\n");
        t = Regex.Replace(t, "[ \t\r\n\u00a0]+", m => m.Value == "\n" ? "\n" : " ");
        t = Regex.Replace(t, " *\n *", "\n");
        return t.Trim();
    }
}
