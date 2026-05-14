using System.Net;
using System.Text;
using StrongTowing.Application.DTOs.Requests;

namespace StrongTowing.API.Services;

/// <summary>
/// Builds an email-client safe HTML body for the "Send quote by email" flow.
/// Layout uses table-based markup + inline styles (Outlook / Gmail safe);
/// the rich PDF preview is sent as an attachment alongside this summary.
/// </summary>
public static class QuoteEmailHtmlBuilder
{
    private static string H(string? s) => WebUtility.HtmlEncode(s ?? "");

    public static string BuildInnerHtml(QuoteEmailRequest req)
    {
        var d = req.Quote;
        var sb = new StringBuilder(8_192);

        var greetingName = !string.IsNullOrWhiteSpace(d.ClientName) ? d.ClientName : null;
        var greeting = greetingName is null ? "Hello," : $"Hello {greetingName},";

        sb.Append("<p style=\"margin:0 0 14px 0;font-size:16px;font-weight:600;color:#0f172a;\">")
          .Append(H(greeting)).Append("</p>");

        var fallbackIntro = string.IsNullOrWhiteSpace(d.CompanyName)
            ? "Thank you for reaching out. Please find your service quote details below."
            : $"Thank you for choosing {d.CompanyName}. Please find your service quote details below.";

        var introText = string.IsNullOrWhiteSpace(req.Message) ? fallbackIntro : req.Message!.Trim();

        sb.Append("<p style=\"margin:0 0 18px 0;font-size:15px;line-height:1.6;color:#0f172a;white-space:pre-line;\">")
          .Append(H(introText)).Append("</p>");

        // Quote header card (ref, dates)
        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" ")
          .Append("style=\"border:1px solid #e2e8f0;border-radius:8px;background:#f8fafc;margin:0 0 18px 0;\">");
        sb.Append("<tr><td style=\"padding:14px 18px;\">");
        sb.Append("<div style=\"font-size:11px;letter-spacing:0.08em;color:#475569;font-weight:600;font-family:system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;\">QUOTE</div>");
        sb.Append("<div style=\"font-size:18px;font-weight:700;color:#0f172a;margin-top:2px;\">")
          .Append(H(string.IsNullOrWhiteSpace(d.QuoteRef) ? "—" : d.QuoteRef)).Append("</div>");

        if (!string.IsNullOrWhiteSpace(d.QuoteDateDisplay) || !string.IsNullOrWhiteSpace(d.ValidUntilDisplay))
        {
            sb.Append("<div style=\"margin-top:8px;font-size:13px;color:#475569;font-family:system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;\">");
            if (!string.IsNullOrWhiteSpace(d.QuoteDateDisplay))
                sb.Append("Date: <strong style=\"color:#0f172a;\">").Append(H(d.QuoteDateDisplay)).Append("</strong>");
            if (!string.IsNullOrWhiteSpace(d.QuoteDateDisplay) && !string.IsNullOrWhiteSpace(d.ValidUntilDisplay))
                sb.Append(" &nbsp;·&nbsp; ");
            if (!string.IsNullOrWhiteSpace(d.ValidUntilDisplay))
                sb.Append("Valid until: <strong style=\"color:#0f172a;\">").Append(H(d.ValidUntilDisplay)).Append("</strong>");
            sb.Append("</div>");
        }
        sb.Append("</td></tr></table>");

        // Service overview
        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" ")
          .Append("style=\"border-collapse:collapse;margin:0 0 16px 0;font-family:system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;font-size:14px;color:#0f172a;\">");
        AppendDetailRow(sb, "Service Type", string.IsNullOrWhiteSpace(d.ServiceType) ? "—" : d.ServiceType);
        AppendDetailRow(sb, "Service Date", string.IsNullOrWhiteSpace(d.ServiceDateDisplay) ? "—" : d.ServiceDateDisplay);
        AppendDetailRow(sb, "Truck Type", string.IsNullOrWhiteSpace(d.TruckTypeLabel) ? "—" : d.TruckTypeLabel);
        AppendDetailRow(sb, "Pickup", string.IsNullOrWhiteSpace(d.Pickup) ? "—" : d.Pickup);
        AppendDetailRow(sb, "Destination", string.IsNullOrWhiteSpace(d.Destination) ? "—" : d.Destination);
        AppendDetailRow(sb, "Estimated Loaded Mileage",
            string.IsNullOrWhiteSpace(d.LoadedMileageDisplay) ? "—" : d.LoadedMileageDisplay);
        sb.Append("</table>");

        // Pricing breakdown
        sb.Append("<div style=\"font-size:11px;letter-spacing:0.08em;color:#475569;font-weight:700;font-family:system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;margin:6px 0 8px 0;\">PRICING BREAKDOWN</div>");
        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" ")
          .Append("style=\"border-collapse:collapse;border:1px solid #e2e8f0;border-radius:8px;overflow:hidden;font-family:system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;font-size:13px;color:#0f172a;margin:0 0 16px 0;\">");
        sb.Append("<thead><tr style=\"background:#1e3a5f;color:#ffffff;\">");
        sb.Append("<th align=\"left\" style=\"padding:10px 12px;font-weight:600;\">Description</th>");
        sb.Append("<th align=\"right\" style=\"padding:10px 12px;font-weight:600;\">Rate</th>");
        sb.Append("<th align=\"right\" style=\"padding:10px 12px;font-weight:600;\">Qty</th>");
        sb.Append("<th align=\"right\" style=\"padding:10px 12px;font-weight:600;\">Amount</th>");
        sb.Append("</tr></thead><tbody>");
        var idx = 0;
        foreach (var item in d.LineItems ?? Enumerable.Empty<QuotePdfLineItemDto>())
        {
            var bg = idx % 2 == 1 ? "#f8fafc" : "#ffffff";
            sb.Append("<tr style=\"background:").Append(bg).Append(";\">");
            sb.Append("<td style=\"padding:10px 12px;border-top:1px solid #e2e8f0;\">");
            sb.Append("<div style=\"font-weight:600;color:#0f172a;\">").Append(H(item.Description)).Append("</div>");
            if (!string.IsNullOrWhiteSpace(item.Subtitle))
                sb.Append("<div style=\"font-size:12px;color:#64748b;margin-top:2px;\">").Append(H(item.Subtitle)).Append("</div>");
            sb.Append("</td>");
            sb.Append("<td align=\"right\" style=\"padding:10px 12px;border-top:1px solid #e2e8f0;color:#475569;\">").Append(H(item.UnitPrice)).Append("</td>");
            sb.Append("<td align=\"right\" style=\"padding:10px 12px;border-top:1px solid #e2e8f0;color:#475569;\">").Append(H(item.Quantity)).Append("</td>");
            sb.Append("<td align=\"right\" style=\"padding:10px 12px;border-top:1px solid #e2e8f0;font-weight:600;color:#0f172a;\">").Append(H(item.Amount)).Append("</td>");
            sb.Append("</tr>");
            idx++;
        }
        sb.Append("</tbody><tfoot>");
        sb.Append("<tr><td colspan=\"3\" align=\"right\" style=\"padding:8px 12px;border-top:1px solid #e2e8f0;color:#475569;\">Subtotal</td>")
          .Append("<td align=\"right\" style=\"padding:8px 12px;border-top:1px solid #e2e8f0;color:#0f172a;\">").Append(H(d.TotalsSubtotal)).Append("</td></tr>");
        sb.Append("<tr><td colspan=\"3\" align=\"right\" style=\"padding:8px 12px;color:#475569;\">").Append(H(d.TotalsTaxLabel)).Append("</td>")
          .Append("<td align=\"right\" style=\"padding:8px 12px;color:#0f172a;\">").Append(H(d.TotalsTax)).Append("</td></tr>");
        sb.Append("<tr style=\"background:#0f172a;color:#ffffff;\"><td colspan=\"3\" align=\"right\" style=\"padding:12px;font-weight:700;letter-spacing:0.04em;\">TOTAL ESTIMATE</td>")
          .Append("<td align=\"right\" style=\"padding:12px;font-weight:700;font-size:15px;\">").Append(H(d.TotalAmount)).Append("</td></tr>");
        sb.Append("</tfoot></table>");

        // Disclaimer
        sb.Append("<p style=\"margin:0 0 14px 0;padding:12px 14px;background:#fff7ed;border:1px solid #fed7aa;border-radius:6px;font-size:13px;color:#7c2d12;font-family:system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;line-height:1.55;\">")
          .Append("This is an estimate only and does not reserve a truck. Final pricing may vary based on actual circumstances at the time of service.")
          .Append("</p>");

        // Attachment hint — the controller always attaches the server-rendered PDF
        // (or the request fails with 503 before this builder runs).
        sb.Append("<p style=\"margin:0 0 16px 0;font-size:13px;color:#475569;font-family:system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;\">")
          .Append("A printable PDF copy of this quote is attached for your records.")
          .Append("</p>");

        // Company contact footer
        var companyName = string.IsNullOrWhiteSpace(d.CompanyName) ? "Strong Towing" : d.CompanyName;
        sb.Append("<p style=\"margin:18px 0 4px 0;font-size:14px;color:#0f172a;font-family:system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;\">If you have any questions or would like to schedule this service, please reply to this email or contact us directly.</p>");
        sb.Append("<p style=\"margin:14px 0 0 0;font-size:13px;color:#475569;font-family:system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;line-height:1.6;\">");
        sb.Append("Best regards,<br/><strong style=\"color:#0f172a;\">").Append(H(companyName)).Append("</strong>");
        if (!string.IsNullOrWhiteSpace(d.CompanyPhone))
            sb.Append("<br/>Phone: ").Append(H(d.CompanyPhone));
        if (!string.IsNullOrWhiteSpace(d.CompanyEmail))
            sb.Append("<br/>Email: ").Append(H(d.CompanyEmail));
        if (!string.IsNullOrWhiteSpace(d.CompanyWebsite))
            sb.Append("<br/>Web: ").Append(H(d.CompanyWebsite));
        sb.Append("</p>");

        return sb.ToString();
    }

    private static void AppendDetailRow(StringBuilder sb, string label, string value)
    {
        sb.Append("<tr>");
        sb.Append("<td style=\"padding:6px 0;color:#64748b;width:42%;vertical-align:top;\">").Append(H(label)).Append("</td>");
        sb.Append("<td style=\"padding:6px 0;color:#0f172a;font-weight:500;vertical-align:top;\">").Append(H(value)).Append("</td>");
        sb.Append("</tr>");
    }

    public static string BuildPlainText(QuoteEmailRequest req)
    {
        var d = req.Quote;
        var sb = new StringBuilder(2_048);

        if (!string.IsNullOrWhiteSpace(d.ClientName))
            sb.Append("Hello ").Append(d.ClientName).Append(',').AppendLine().AppendLine();
        else
            sb.AppendLine("Hello,").AppendLine();

        if (!string.IsNullOrWhiteSpace(req.Message))
            sb.AppendLine(req.Message!.Trim()).AppendLine();
        else
        {
            var co = string.IsNullOrWhiteSpace(d.CompanyName) ? "us" : d.CompanyName;
            sb.Append("Thank you for choosing ").Append(co).AppendLine(". Please find your service quote details below.").AppendLine();
        }

        sb.Append("Quote: ").AppendLine(string.IsNullOrWhiteSpace(d.QuoteRef) ? "—" : d.QuoteRef);
        if (!string.IsNullOrWhiteSpace(d.QuoteDateDisplay))
            sb.Append("Date: ").AppendLine(d.QuoteDateDisplay);
        if (!string.IsNullOrWhiteSpace(d.ValidUntilDisplay))
            sb.Append("Valid Until: ").AppendLine(d.ValidUntilDisplay);
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(d.ServiceType)) sb.Append("Service Type: ").AppendLine(d.ServiceType);
        if (!string.IsNullOrWhiteSpace(d.TruckTypeLabel)) sb.Append("Truck Type: ").AppendLine(d.TruckTypeLabel);
        if (!string.IsNullOrWhiteSpace(d.Pickup)) sb.Append("Pickup: ").AppendLine(d.Pickup);
        if (!string.IsNullOrWhiteSpace(d.Destination)) sb.Append("Destination: ").AppendLine(d.Destination);
        if (!string.IsNullOrWhiteSpace(d.LoadedMileageDisplay))
            sb.Append("Estimated Loaded Mileage: ").AppendLine(d.LoadedMileageDisplay);
        sb.AppendLine();

        sb.AppendLine("Pricing breakdown:");
        foreach (var item in d.LineItems ?? Enumerable.Empty<QuotePdfLineItemDto>())
            sb.Append("  • ").Append(item.Description).Append("  Qty ").Append(item.Quantity)
              .Append(" @ ").Append(item.UnitPrice).Append("  =  ").AppendLine(item.Amount);

        sb.AppendLine();
        sb.Append("Subtotal: ").AppendLine(d.TotalsSubtotal);
        sb.Append(d.TotalsTaxLabel).Append(": ").AppendLine(d.TotalsTax);
        sb.Append("TOTAL ESTIMATE: ").AppendLine(d.TotalAmount);
        sb.AppendLine();
        sb.AppendLine("This is an estimate only and does not reserve a truck. Final pricing may vary based on actual circumstances at the time of service.");
        sb.AppendLine();

        sb.AppendLine("A PDF copy of this quote is attached for your records.").AppendLine();

        var companyName = string.IsNullOrWhiteSpace(d.CompanyName) ? "Strong Towing" : d.CompanyName;
        sb.AppendLine("Best regards,");
        sb.AppendLine(companyName);
        if (!string.IsNullOrWhiteSpace(d.CompanyPhone)) sb.Append("Phone: ").AppendLine(d.CompanyPhone);
        if (!string.IsNullOrWhiteSpace(d.CompanyEmail)) sb.Append("Email: ").AppendLine(d.CompanyEmail);
        if (!string.IsNullOrWhiteSpace(d.CompanyWebsite)) sb.Append("Web: ").AppendLine(d.CompanyWebsite);

        return sb.ToString();
    }
}
