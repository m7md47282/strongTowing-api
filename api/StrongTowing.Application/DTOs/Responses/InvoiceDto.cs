namespace StrongTowing.Application.DTOs.Responses;

public class InvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssuedDate { get; set; }
    public DateTime? DueDate { get; set; }

    public string ClientName { get; set; } = string.Empty;
    public string? ClientPhone { get; set; }
    public string? ClientEmail { get; set; }
    public string? ClientAddress { get; set; }

    public int? JobId { get; set; }

    public decimal TaxRate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }

    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;

    public bool HideLogo { get; set; }
    public string? CustomLogoUrl { get; set; }
    public bool HideCompanyName { get; set; }
    public string? CompanyDisplayName { get; set; }

    public string? CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<InvoiceLineItemDto> LineItems { get; set; } = new();

    public List<InvoiceImageDto> Images { get; set; } = new();
}

public class InvoiceImageDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class InvoiceLineItemDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int SortOrder { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? UnitType { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal Discount { get; set; }
    public bool IsDiscountPercentage { get; set; }
    public bool IsTaxable { get; set; }
    public decimal Total { get; set; }
}
