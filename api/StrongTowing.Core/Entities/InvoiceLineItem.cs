using System.ComponentModel.DataAnnotations.Schema;

namespace StrongTowing.Core.Entities
{
    public class InvoiceLineItem
    {
        public int Id { get; set; }

        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;

        public int SortOrder { get; set; }

        public string ServiceName { get; set; } = string.Empty;
        public string? Details { get; set; }

        // Category: Services | Materials | Other
        public string Category { get; set; } = "Services";

        public string? UnitType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }

        public bool IsDiscountPercentage { get; set; }

        public bool IsTaxable { get; set; } = true;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }
    }
}
