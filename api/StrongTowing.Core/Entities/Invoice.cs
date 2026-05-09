using System.ComponentModel.DataAnnotations.Schema;

namespace StrongTowing.Core.Entities
{
    public class Invoice
    {
        public int Id { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty; // e.g. INV-130

        public DateTime IssuedDate { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }

        // Client info (freeform — not linked to ApplicationUser)
        public string ClientName { get; set; } = string.Empty;
        public string? ClientPhone { get; set; }
        public string? ClientEmail { get; set; }
        public string? ClientAddress { get; set; }

        // Optional link to an existing job
        public int? JobId { get; set; }
        public Job? Job { get; set; }

        // Financials
        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxRate { get; set; } = 7.5m; // percentage

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        public string? Notes { get; set; }

        // Status: Draft | Sent | Paid | Cancelled
        public string Status { get; set; } = "Draft";

        /// <summary>When true, printed invoice shows no logo (ignores <see cref="CustomLogoUrl"/>).</summary>
        public bool HideLogo { get; set; }

        /// <summary>Stored path e.g. /uploads/invoice-logos/{id}/file — used when <see cref="HideLogo"/> is false.</summary>
        public string? CustomLogoUrl { get; set; }

        /// <summary>When true, the first line of the “FROM” company name is omitted on the printed invoice.</summary>
        public bool HideCompanyName { get; set; }

        /// <summary>Optional override for the company name line; when null/empty, default branding is used.</summary>
        public string? CompanyDisplayName { get; set; }

        // Audit
        public string? CreatedById { get; set; }
        public ApplicationUser? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();

        public ICollection<InvoiceImage> Images { get; set; } = new List<InvoiceImage>();
    }
}
