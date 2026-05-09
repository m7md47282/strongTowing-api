using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class UpdateInvoiceRequest
{
    public DateTime IssuedDate { get; set; }
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

    /// <summary>Remove custom logo file and fall back to default or hidden per <see cref="HideLogo"/>.</summary>
    public bool ClearCustomLogo { get; set; }

    public List<CreateInvoiceLineItemRequest> LineItems { get; set; } = new();
}

public class UpdateInvoiceStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}
