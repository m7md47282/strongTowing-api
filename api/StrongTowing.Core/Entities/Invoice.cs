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

        // Audit
        public string? CreatedById { get; set; }
        public ApplicationUser? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
    }
}
