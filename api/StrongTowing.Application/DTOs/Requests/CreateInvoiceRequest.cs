using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class CreateInvoiceRequest
{
    public DateTime IssuedDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }

    [Required]
    public string ClientName { get; set; } = string.Empty;
    public string? ClientPhone { get; set; }
    public string? ClientEmail { get; set; }
    public string? ClientAddress { get; set; }

    public int? JobId { get; set; }

    public decimal TaxRate { get; set; } = 7.5m;
    public string? Notes { get; set; }
    public string Status { get; set; } = "Draft";

    public bool HideLogo { get; set; }
    public bool HideCompanyName { get; set; }
    public string? CompanyDisplayName { get; set; }

    public List<CreateInvoiceLineItemRequest> LineItems { get; set; } = new();
}

public class CreateInvoiceLineItemRequest
{
    [Required]
    public string ServiceName { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string Category { get; set; } = "Services";
    public string? UnitType { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal Discount { get; set; }
    public bool IsDiscountPercentage { get; set; }
    public bool IsTaxable { get; set; } = true;
    public int SortOrder { get; set; }
}
