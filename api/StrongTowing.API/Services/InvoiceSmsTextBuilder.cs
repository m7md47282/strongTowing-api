using System.Text;
using StrongTowing.Core.Entities;

namespace StrongTowing.API.Services;

public static class InvoiceSmsTextBuilder
{
    private const int MaxBodyLength = 1600;
    private const int MaxLineItemsShown = 8;
    private const string Divider = "━━━━━━━━━━━━━━━━";

    public static string Build(Invoice invoice)
    {
        var sb = new StringBuilder(512);
        sb.AppendLine("Strong Towing — Invoice");
        sb.AppendLine($"Ref: {invoice.InvoiceNumber}");
        sb.AppendLine($"Issued: {invoice.IssuedDate:MM/dd/yyyy}");
        if (invoice.DueDate.HasValue)
        {
            sb.AppendLine($"Due: {invoice.DueDate.Value:MM/dd/yyyy}");
        }

        sb.AppendLine();
        sb.Append("Hi ").Append(invoice.ClientName.Trim()).AppendLine(",");
        sb.AppendLine();
        sb.AppendLine("Line items:");

        var items = invoice.LineItems.OrderBy(li => li.SortOrder).ToList();
        foreach (var li in items.Take(MaxLineItemsShown))
        {
            sb.Append("• ").Append(li.ServiceName.Trim());
            sb.Append(" x").Append(li.Quantity.ToString("0.##"));
            sb.Append(": $").AppendLine(li.Total.ToString("0.00"));
        }

        if (items.Count > MaxLineItemsShown)
        {
            sb.AppendLine($"(+{items.Count - MaxLineItemsShown} more — see printed invoice)");
        }

        sb.AppendLine();
        sb.Append("Subtotal: $").AppendLine(invoice.SubTotal.ToString("0.00"));
        sb.Append("Tax: $").AppendLine(invoice.TaxAmount.ToString("0.00"));
        sb.AppendLine(Divider);
        sb.Append("TOTAL: $").AppendLine(invoice.Total.ToString("0.00"));
        sb.AppendLine(Divider);
        sb.AppendLine();
        sb.AppendLine("Questions? Call (703) 200-8836.");

        var text = sb.ToString();
        if (text.Length <= MaxBodyLength)
        {
            return text;
        }

        return text[..(MaxBodyLength - 50)] + "\n\n… (truncated — use Print for full invoice.)";
    }
}
