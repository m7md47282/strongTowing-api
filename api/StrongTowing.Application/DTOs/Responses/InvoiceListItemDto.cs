namespace StrongTowing.Application.DTOs.Responses;

public class InvoiceListItemDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssuedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string? ClientPhone { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LineItemCount { get; set; }
}
