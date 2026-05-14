namespace StrongTowing.Application.DTOs.Requests;

/// <summary>
/// Request payload for emailing a quote to a client. The PDF attachment is rendered
/// server-side by the same Playwright/Chromium pipeline that powers <c>POST /api/quotes/pdf</c>
/// so icons, Font Awesome glyphs and SVG payment marks stay crisp and vector.
/// </summary>
public sealed class QuoteEmailRequest
{
    /// <summary>Recipient email; user can edit before confirming send.</summary>
    public string ToEmail { get; set; } = "";

    /// <summary>Custom subject. Falls back to <c>Quote {QuoteRef} — {CompanyName}</c> when blank.</summary>
    public string? Subject { get; set; }

    /// <summary>Optional dispatcher note rendered above the quote summary in the email body.</summary>
    public string? Message { get; set; }

    /// <summary>Quote summary used to render both the email body and the attached PDF.</summary>
    public QuotePdfRequest Quote { get; set; } = new();
}
