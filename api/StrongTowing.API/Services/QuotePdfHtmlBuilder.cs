using System.Net;
using System.Text;
using StrongTowing.Application.DTOs.Requests;

namespace StrongTowing.API.Services;

/// <summary>Builds a self-contained HTML document for Chromium PDF (Playwright).</summary>
public static class QuotePdfHtmlBuilder
{
    private static string H(string? s) => WebUtility.HtmlEncode(s ?? "");

    public static string ServiceLocationLabel(QuotePdfRequest d)
    {
        var v = (d.LoadedMileageDisplay ?? "").Trim();
        if (string.IsNullOrEmpty(v) || v == "—" || v == "-")
            return "—";
        var cleaned = v.Replace(" MI", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" mi", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (double.TryParse(cleaned.Replace(",", ""), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var miles))
            return miles <= 50 ? "Within Service Area" : "Outside Service Area";
        return "Within Service Area";
    }

    public static string Build(QuotePdfRequest d, string assetBase)
    {
        assetBase = assetBase.TrimEnd('/');
        var qa = assetBase + "/quote-assets";
        var svcLoc = ServiceLocationLabel(d);

        var sb = new StringBuilder(48_000);
        sb.AppendLine("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\"/>");
        sb.AppendLine(
            "<link rel=\"stylesheet\" href=\"https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css\" crossorigin=\"anonymous\" referrerpolicy=\"no-referrer\"/>");
        sb.Append("<style>");
        sb.Append(Css);
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<article class=\"qs\">");

        // Header
        sb.AppendLine("<header class=\"qs__header\"><div class=\"qs__brand\"><div class=\"qs__brand-top\">");
        sb.Append("<img class=\"qs__logo\" src=\"").Append(qa).Append("/logo.svg\" alt=\"")
            .Append(H(d.CompanyName)).Append("\"/>");
        sb.AppendLine("<div class=\"qs__powered\"><span class=\"qs__powered-label\">POWERED BY</span>");
        sb.AppendLine("<i class=\"fab fa-swift qs__powered-icon\"></i><span class=\"qs__powered-name\">SWIFT</span></div></div>");
        sb.AppendLine("<div class=\"qs__brand-contact\">");
        if (!string.IsNullOrWhiteSpace(d.CompanyAddress))
        {
            sb.AppendLine("<span class=\"qs__contact-row\"><span class=\"qs__icon-cell\"><i class=\"fas fa-fw fa-location-dot qs__icon\"></i></span>");
            sb.Append("<span class=\"qs__contact-text\">").Append(H(d.CompanyAddress)).AppendLine("</span></span>");
        }
        if (!string.IsNullOrWhiteSpace(d.CompanyPhone))
        {
            sb.AppendLine("<span class=\"qs__contact-row\"><span class=\"qs__icon-cell\"><i class=\"fas fa-fw fa-phone qs__icon\"></i></span>");
            sb.Append("<span class=\"qs__contact-text\">").Append(H(d.CompanyPhone)).AppendLine("</span></span>");
        }
        if (!string.IsNullOrWhiteSpace(d.CompanyEmail))
        {
            sb.AppendLine("<span class=\"qs__contact-row\"><span class=\"qs__icon-cell\"><i class=\"fas fa-fw fa-envelope qs__icon\"></i></span>");
            sb.Append("<span class=\"qs__contact-text\">").Append(H(d.CompanyEmail)).AppendLine("</span></span>");
        }
        sb.AppendLine("</div></div>");
        sb.AppendLine("<div class=\"qs__meta\"><div class=\"qs__meta-title\">QUOTE</div>");
        sb.Append("<div class=\"qs__meta-ref\">").Append(H(d.QuoteRef)).AppendLine("</div>");
        sb.AppendLine("<div class=\"qs__meta-dates\">");
        sb.Append("<div class=\"qs__meta-date-row\"><span class=\"qs__meta-date-label\">Date:</span><span class=\"qs__meta-date-val\">")
            .Append(H(d.QuoteDateDisplay)).AppendLine("</span></div>");
        sb.Append("<div class=\"qs__meta-date-row\"><span class=\"qs__meta-date-label\">Valid Until:</span><span class=\"qs__meta-date-val\">")
            .Append(H(d.ValidUntilDisplay)).AppendLine("</span></div>");
        sb.AppendLine("</div></div></header>");

        // Banners
        sb.AppendLine("<div class=\"qs__banners\">");
        sb.AppendLine("<div class=\"qs__banner qs__banner--dark\"><span class=\"qs__banner-icon-ring qs__banner-icon-ring--on-dark\">");
        sb.AppendLine("<i class=\"fas fa-fw fa-circle-info qs__banner-icon\"></i></span>");
        sb.AppendLine("<p class=\"qs__banner-text qs__banner-text--on-dark\">This is an estimate only and does not reserve a truck.<br/>");
        sb.AppendLine("Prices may vary based on actual circumstances.</p></div>");
        sb.AppendLine("<div class=\"qs__banner qs__banner--light\"><span class=\"qs__banner-icon-ring qs__banner-icon-ring--on-light\">");
        sb.AppendLine("<i class=\"fas fa-fw fa-dollar-sign qs__banner-icon\"></i></span><div class=\"qs__banner-body\">");
        sb.AppendLine("<div class=\"qs__banner-heading\">How pricing works</div>");
        sb.AppendLine("<p class=\"qs__banner-text qs__banner-text--on-light\">We calculate your total using our service rates below and the details of your job.</p>");
        sb.AppendLine("</div></div></div>");

        // Info grid
        sb.AppendLine("<div class=\"qs__info-grid\">");
        sb.AppendLine("<div class=\"qs__info-card\"><div class=\"qs__info-card-header\">");
        sb.AppendLine("<span class=\"qs__info-icon-cell\"><i class=\"fas fa-fw fa-user qs__info-icon\"></i></span>");
        sb.AppendLine("<span class=\"qs__info-card-heading\">QUOTE FOR</span></div><div class=\"qs__info-card-body\">");
        sb.AppendLine("<div class=\"qs__field-label\">Client</div>");
        sb.Append("<div class=\"qs__field-value qs__field-value--bold\">").Append(H(d.ClientDisplayLine)).AppendLine("</div></div></div>");

        sb.AppendLine("<div class=\"qs__info-card\"><div class=\"qs__info-card-header\">");
        sb.AppendLine("<span class=\"qs__info-icon-cell\"><i class=\"fas fa-fw fa-clipboard-list qs__info-icon\"></i></span>");
        sb.AppendLine("<span class=\"qs__info-card-heading\">SERVICE OVERVIEW</span></div><div class=\"qs__info-card-body\">");
        sb.Append("<div class=\"qs__overview-row\"><span class=\"qs__overview-label\">Service Type:</span><span class=\"qs__overview-val\">")
            .Append(H(string.IsNullOrWhiteSpace(d.ServiceType) ? "—" : d.ServiceType)).AppendLine("</span></div>");
        sb.Append("<div class=\"qs__overview-row\"><span class=\"qs__overview-label\">Service Date:</span><span class=\"qs__overview-val\">")
            .Append(H(d.ServiceDateDisplay)).AppendLine("</span></div>");
        sb.Append("<div class=\"qs__overview-row\"><span class=\"qs__overview-label\">Truck Type:</span><span class=\"qs__overview-val\">")
            .Append(H(d.TruckTypeLabel)).AppendLine("</span></div>");
        sb.Append("<div class=\"qs__overview-row\"><span class=\"qs__overview-label\">Service Location:</span><span class=\"qs__overview-val\">")
            .Append(H(svcLoc)).AppendLine("</span></div>");
        sb.AppendLine("</div></div></div>");

        // Location
        sb.AppendLine("<section class=\"qs__location\"><div class=\"qs__section-header\">");
        sb.AppendLine("<span class=\"qs__section-icon-wrap\"><i class=\"fas fa-fw fa-location-dot qs__section-fa\"></i></span>");
        sb.AppendLine("<span class=\"qs__section-title\"><span class=\"qs__section-title-inner\">LOCATION DETAILS</span></span></div>");
        sb.AppendLine("<div class=\"qs__location-body\">");
        sb.AppendLine("<div class=\"qs__loc-col qs__loc-col--pickup\"><div class=\"qs__loc-label\">PICKUP</div>");
        sb.Append("<div class=\"qs__loc-addr\">").Append(H(string.IsNullOrWhiteSpace(d.Pickup) ? "—" : d.Pickup)).AppendLine("</div></div>");
        sb.AppendLine("<div class=\"qs__loc-route-column\"><div class=\"qs__loc-route-track\">");
        sb.AppendLine("<i class=\"fas fa-fw fa-location-dot qs__route-pin\"></i><span class=\"qs__route-dash\"></span>");
        sb.Append("<img class=\"qs__route-truck\" src=\"").Append(qa).Append("/towing-icon.jpg\" alt=\"\"/>");
        sb.AppendLine("<span class=\"qs__route-dash\"></span><i class=\"fas fa-fw fa-location-dot qs__route-pin\"></i></div>");
        sb.AppendLine("<div class=\"qs__loc-mileage\"><span class=\"qs__mileage-icon-cell\"><i class=\"fas fa-fw fa-road qs__mileage-fa\"></i></span>");
        sb.Append("<span class=\"qs__mileage-copy\"><span class=\"qs__mileage-label\">ESTIMATED LOADED MILEAGE:</span><strong>")
            .Append(H(d.LoadedMileageDisplay)).AppendLine("</strong></span></div></div>");
        sb.AppendLine("<div class=\"qs__loc-col qs__loc-col--dest\"><div class=\"qs__loc-label\">DESTINATION</div>");
        sb.Append("<div class=\"qs__loc-addr\">").Append(H(string.IsNullOrWhiteSpace(d.Destination) ? "—" : d.Destination)).AppendLine("</div></div>");
        sb.AppendLine("</div></section>");

        // Table
        sb.AppendLine("<section class=\"qs__pricing\"><div class=\"qs__section-header\">");
        sb.AppendLine("<span class=\"qs__section-icon-wrap\"><i class=\"fas fa-fw fa-tags qs__section-fa\"></i></span>");
        sb.AppendLine("<span class=\"qs__section-title\"><span class=\"qs__section-title-inner\">PRICING BREAKDOWN</span></span></div>");
        sb.AppendLine("<table class=\"qs__table\"><thead><tr>");
        sb.AppendLine("<th class=\"qs__th qs__th--desc\">DESCRIPTION</th><th class=\"qs__th qs__th--num\">RATE</th>");
        sb.AppendLine("<th class=\"qs__th qs__th--num\">QTY / MILES</th><th class=\"qs__th qs__th--num\">AMOUNT</th></tr></thead><tbody class=\"qs__tbody\">");
        var idx = 0;
        foreach (var item in d.LineItems ?? Enumerable.Empty<QuotePdfLineItemDto>())
        {
            var alt = idx % 2 == 1 ? " qs__tr--alt" : "";
            sb.Append("<tr class=\"qs__tr").Append(alt).AppendLine("\">");
            sb.Append("<td class=\"qs__td qs__td--desc\"><span class=\"qs__item-name\">").Append(H(item.Description)).Append("</span>");
            if (!string.IsNullOrWhiteSpace(item.Subtitle))
                sb.Append("<span class=\"qs__item-sub\">").Append(H(item.Subtitle)).Append("</span>");
            sb.AppendLine("</td>");
            sb.Append("<td class=\"qs__td qs__td--num qs__num\">").Append(H(item.UnitPrice)).AppendLine("</td>");
            sb.Append("<td class=\"qs__td qs__td--num qs__num\">").Append(H(item.Quantity)).AppendLine("</td>");
            sb.Append("<td class=\"qs__td qs__td--num qs__num\">").Append(H(item.Amount)).AppendLine("</td></tr>");
            idx++;
        }
        sb.AppendLine("</tbody><tfoot>");
        sb.Append("<tr class=\"qs__tfoot-row\"><td colspan=\"3\" class=\"qs__tfoot-label\">Subtotal</td><td class=\"qs__tfoot-val qs__num\">")
            .Append(H(d.TotalsSubtotal)).AppendLine("</td></tr>");
        sb.Append("<tr class=\"qs__tfoot-row\"><td colspan=\"3\" class=\"qs__tfoot-label\">").Append(H(d.TotalsTaxLabel))
            .Append("</td><td class=\"qs__tfoot-val qs__num\">").Append(H(d.TotalsTax)).AppendLine("</td></tr>");
        sb.AppendLine("<tr class=\"qs__tfoot-row qs__tfoot-row--total\">");
        sb.AppendLine("<td colspan=\"3\" class=\"qs__tfoot-label qs__tfoot-label--total\">TOTAL ESTIMATE</td>");
        sb.Append("<td class=\"qs__tfoot-val qs__tfoot-val--total qs__num\">").Append(H(d.TotalAmount)).AppendLine("</td></tr>");
        sb.AppendLine("</tfoot></table></section>");

        // Footer grid
        sb.AppendLine("<div class=\"qs__footer-grid\">");
        sb.AppendLine("<div class=\"qs__footer-card\"><div class=\"qs__footer-card-title\">");
        sb.AppendLine("<span class=\"qs__footer-icon-cell\"><i class=\"fas fa-fw fa-file-lines qs__footer-icon\"></i></span>");
        sb.AppendLine("<span class=\"qs__footer-heading-text\">IMPORTANT INFORMATION</span></div><ul class=\"qs__footer-list\">");
        sb.AppendLine("<li>This is an estimate only and the final price may change based on actual circumstances.</li>");
        sb.AppendLine("<li>Payment is due upon completion of the service.</li>");
        sb.AppendLine("<li>Additional charges may apply for storage, wait time, or service outside the quoted details.</li>");
        sb.AppendLine("<li>Cancellations may be subject to a dispatch fee.</li></ul></div>");

        sb.AppendLine("<div class=\"qs__footer-card\"><div class=\"qs__footer-card-title\">");
        sb.AppendLine("<span class=\"qs__footer-icon-cell\"><i class=\"fas fa-fw fa-credit-card qs__footer-icon\"></i></span>");
        sb.AppendLine("<span class=\"qs__footer-heading-text\">PAYMENT METHODS</span></div>");
        sb.AppendLine("<p class=\"qs__footer-text\">We accept the following payment methods:</p>");
        sb.AppendLine("<div class=\"qs__payment-methods\">");
        void Pay(string file, string title)
        {
            sb.Append("<div class=\"qs__payment-card\" title=\"").Append(H(title)).Append("\"><img src=\"")
                .Append(qa).Append('/').Append(file).Append("\" alt=\"").Append(H(title)).AppendLine("\" loading=\"eager\"/></div>");
        }
        Pay("visa.svg", "Visa");
        Pay("mastercard.svg", "Mastercard");
        Pay("americanexpress.svg", "American Express");
        Pay("discover.svg", "Discover");
        Pay("applepay.svg", "Apple Pay");
        Pay("zelle.svg", "Zelle");
        Pay("cashapp.svg", "Cash App");
        sb.AppendLine("<div class=\"qs__payment-card qs__payment-card--cash\" title=\"Cash\">");
        sb.AppendLine("<i class=\"fas fa-money-bill-wave qs__payment-cash-icon\"></i><span class=\"qs__payment-cash-text\">Cash</span></div>");
        sb.AppendLine("</div></div>");

        sb.AppendLine("<div class=\"qs__footer-card\"><div class=\"qs__footer-card-title\">");
        sb.AppendLine("<span class=\"qs__footer-icon-cell\"><i class=\"fas fa-fw fa-handshake qs__footer-icon\"></i></span>");
        sb.AppendLine("<span class=\"qs__footer-heading-text\">THANK YOU!</span></div>");
        sb.AppendLine("<p class=\"qs__footer-text\">We appreciate your business and the opportunity to serve you.</p>");
        sb.AppendLine("<p class=\"qs__footer-text\">If you have any questions, please contact us anytime.</p></div></div>");

        // Signature
        sb.AppendLine("<div class=\"qs__signature\">");
        sb.AppendLine("<div class=\"qs__sig-field\"><span class=\"qs__sig-label\">Authorized By:</span><span class=\"qs__sig-line\"></span></div>");
        sb.AppendLine("<div class=\"qs__sig-field\"><span class=\"qs__sig-label\">Date:</span><span class=\"qs__sig-line\"></span></div></div>");

        // Bottom bar
        sb.AppendLine("<footer class=\"qs__bottom-bar\">");
        if (!string.IsNullOrWhiteSpace(d.CompanyAddress))
        {
            sb.AppendLine("<span class=\"qs__bottom-item\"><span class=\"qs__bottom-icon-cell\"><i class=\"fas fa-fw fa-location-dot qs__bottom-icon\"></i></span>");
            sb.Append("<span class=\"qs__bottom-text\">").Append(H(d.CompanyAddress)).AppendLine("</span></span>");
        }
        if (!string.IsNullOrWhiteSpace(d.CompanyPhone))
        {
            sb.AppendLine("<span class=\"qs__bottom-item\"><span class=\"qs__bottom-icon-cell\"><i class=\"fas fa-fw fa-phone qs__bottom-icon\"></i></span>");
            sb.Append("<span class=\"qs__bottom-text\">").Append(H(d.CompanyPhone)).AppendLine("</span></span>");
        }
        if (!string.IsNullOrWhiteSpace(d.CompanyEmail))
        {
            sb.AppendLine("<span class=\"qs__bottom-item\"><span class=\"qs__bottom-icon-cell\"><i class=\"fas fa-fw fa-envelope qs__bottom-icon\"></i></span>");
            sb.Append("<span class=\"qs__bottom-text\">").Append(H(d.CompanyEmail)).AppendLine("</span></span>");
        }
        if (!string.IsNullOrWhiteSpace(d.CompanyWebsite))
        {
            sb.AppendLine("<span class=\"qs__bottom-item\"><span class=\"qs__bottom-icon-cell\"><i class=\"fas fa-fw fa-globe qs__bottom-icon\"></i></span>");
            sb.Append("<span class=\"qs__bottom-text\">").Append(H(d.CompanyWebsite)).AppendLine("</span></span>");
        }
        sb.AppendLine("</footer></article></body></html>");
        return sb.ToString();
    }

    private const string Css = """
*{box-sizing:border-box;}
body{margin:0;padding:0;font-family:system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;font-size:13px;color:#0f172a;background:#fff;-webkit-print-color-adjust:exact;print-color-adjust:exact;}
.qs{max-width:860px;margin:0 auto;padding:28px 32px 0;line-height:1.45;background:#fff;}
.qs__header{display:flex;justify-content:space-between;align-items:flex-start;gap:20px;margin-bottom:18px;}
.qs__brand{display:flex;flex-direction:column;gap:8px;}
.qs__logo{height:40px;width:auto;max-width:200px;}
.qs__powered{display:flex;align-items:center;gap:5px;font-size:9px;letter-spacing:0.08em;color:#475569;}
.qs__powered-name{font-weight:800;color:#2563eb;}
.qs__powered-icon{font-size:12px;color:#2563eb;}
.qs__brand-contact{display:flex;flex-direction:column;gap:4px;}
.qs__contact-row{display:flex;align-items:flex-start;gap:8px;font-size:11px;color:#475569;}
.qs__icon-cell{display:flex;align-items:center;justify-content:center;flex-shrink:0;width:16px;height:16px;margin-top:2px;}
.qs__icon{font-size:11px;color:#003366;}
.qs__meta{text-align:right;}
.qs__meta-title{font-size:28px;font-weight:800;color:#003366;line-height:1;}
.qs__meta-ref{font-size:13px;font-weight:600;color:#334155;margin-top:6px;}
.qs__meta-dates{margin-top:10px;font-size:11px;color:#475569;}
.qs__meta-date-row{display:flex;justify-content:flex-end;gap:6px;}
.qs__banners{display:flex;gap:0;margin-bottom:16px;border-radius:8px;overflow:hidden;border:1px solid #e2e8f0;}
.qs__banner{display:flex;align-items:center;gap:10px;padding:10px 14px;}
.qs__banner--dark{flex:1.5;background:#003355;color:#fff;}
.qs__banner--light{flex:1;background:#f5f5f5;color:#334155;}
.qs__banner-icon-ring{display:flex;align-items:center;justify-content:center;width:28px;height:28px;border-radius:9999px;flex-shrink:0;}
.qs__banner-icon-ring--on-dark{background:rgba(255,255,255,0.15);}
.qs__banner-icon-ring--on-light{background:#e2e8f0;}
.qs__banner-icon{font-size:12px;}
.qs__banner-text--on-dark{margin:0;font-size:10.5px;line-height:1.45;}
.qs__banner-heading{font-weight:700;font-size:10px;margin-bottom:4px;text-transform:uppercase;letter-spacing:0.04em;}
.qs__banner-text--on-light{margin:0;font-size:10.5px;}
.qs__info-grid{display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-bottom:16px;}
.qs__info-card{background:#fff;border:1px solid #e2e8f0;border-radius:8px;padding:12px 14px;}
.qs__info-card-header{display:flex;align-items:center;gap:8px;margin-bottom:8px;}
.qs__info-card-heading{font-size:11px;font-weight:700;letter-spacing:0.06em;color:#003366;}
.qs__info-icon{font-size:12px;color:#003366;}
.qs__field-label{font-size:10px;color:#64748b;margin-bottom:4px;}
.qs__field-value--bold{font-weight:600;font-size:12px;}
.qs__overview-row{display:flex;justify-content:space-between;gap:8px;font-size:11px;padding:3px 0;border-bottom:1px solid #f1f5f9;}
.qs__overview-label{color:#64748b;}
.qs__overview-val{font-weight:500;text-align:right;}
.qs__location{border:1px solid #e2e8f0;border-radius:8px;padding:12px 14px;margin-bottom:16px;}
.qs__section-header{display:flex;align-items:center;gap:8px;margin-bottom:10px;}
.qs__section-fa{color:#003366;font-size:13px;}
.qs__section-title-inner{font-size:11px;font-weight:700;letter-spacing:0.06em;color:#003366;}
.qs__location-body{display:grid;grid-template-columns:1fr auto 1fr;gap:12px;align-items:start;}
.qs__loc-label{font-size:9px;font-weight:700;letter-spacing:0.08em;color:#64748b;margin-bottom:4px;}
.qs__loc-addr{font-size:11px;line-height:1.4;}
.qs__loc-route-column{display:flex;flex-direction:column;align-items:center;gap:8px;}
.qs__loc-route-track{display:flex;align-items:center;gap:4px;width:100%;justify-content:center;}
.qs__route-dash{flex:1;height:0;border-top:2px dashed #cbd5e1;max-width:48px;}
.qs__route-pin{color:#003366;font-size:11px;}
.qs__route-truck{width:28px;height:28px;object-fit:contain;border-radius:4px;}
.qs__loc-mileage{display:inline-flex;align-items:center;gap:6px;background:#e0f2fe;border:1px solid #7dd3fc;border-radius:9999px;padding:6px 12px;font-size:10px;}
.qs__mileage-label{font-weight:600;margin-right:4px;}
.qs__pricing{margin-bottom:16px;}
.qs__table{width:100%;border-collapse:collapse;font-size:11.5px;border-radius:8px;overflow:hidden;border:1px solid #e2e8f0;}
.qs__th{background:#003366;color:#fff;font-weight:600;font-size:10px;letter-spacing:0.06em;padding:8px 12px;text-align:left;}
.qs__th--num{text-align:right;padding-right:14px;}
.qs__tbody .qs__tr{border-bottom:1px solid #e2e8f0;}
.qs__tr--alt td{background:#f8fafc;}
.qs__td{padding:10px 12px;vertical-align:top;color:#334155;}
.qs__td--num{text-align:right;white-space:nowrap;padding-right:14px;}
.qs__item-name{display:block;font-weight:600;color:#0f172a;}
.qs__item-sub{display:block;font-size:10px;color:#475569;margin-top:2px;}
.qs__tfoot-row td{padding:8px 12px;font-size:11px;}
.qs__tfoot-label{text-align:right;font-weight:600;color:#334155;}
.qs__tfoot-val{text-align:right;font-weight:600;}
.qs__tfoot-row--total td{background:#003366;color:#fff;font-weight:700;}
.qs__tfoot-label--total{text-align:right;}
.qs__tfoot-val--total{text-align:right;}
.qs__footer-grid{display:grid;grid-template-columns:repeat(3,1fr);gap:12px;margin-bottom:16px;}
.qs__footer-card{border:1px solid #e2e8f0;border-radius:8px;padding:12px;background:#fff;}
.qs__footer-card-title{display:flex;align-items:center;gap:8px;margin-bottom:8px;}
.qs__footer-heading-text{font-size:10px;font-weight:700;letter-spacing:0.06em;color:#003366;}
.qs__footer-icon{font-size:12px;color:#003366;}
.qs__footer-list{margin:0;padding-left:16px;font-size:10px;color:#475569;line-height:1.5;}
.qs__footer-text{font-size:10px;color:#64748b;margin:0 0 6px;line-height:1.5;}
.qs__payment-methods{display:grid;grid-template-columns:repeat(4,1fr);gap:8px;}
.qs__payment-card{display:flex;align-items:center;justify-content:center;border:1px solid #e2e8f0;border-radius:6px;padding:8px;min-height:40px;background:#fff;}
.qs__payment-card img{max-height:24px;max-width:100%;object-fit:contain;}
.qs__payment-card--cash{gap:5px;font-size:10px;font-weight:600;color:#334155;}
.qs__payment-cash-icon{color:#003366;}
.qs__signature{display:flex;justify-content:space-between;gap:32px;padding:14px 0;}
.qs__sig-field{display:flex;align-items:flex-end;gap:8px;width:240px;}
.qs__sig-label{font-size:11px;font-weight:600;color:#334155;white-space:nowrap;}
.qs__sig-line{flex:1;border-bottom:1px solid #94a3b8;margin-bottom:1px;}
.qs__bottom-bar{display:flex;flex-wrap:wrap;gap:10px 16px;background:#003366;color:rgba(255,255,255,0.92);font-size:10px;padding:12px 20px;margin:0 -32px 0;}
.qs__bottom-item{display:flex;align-items:flex-start;gap:8px;flex:1 1 140px;}
.qs__bottom-icon{font-size:11px;color:rgba(255,255,255,0.95);}
""";
}
