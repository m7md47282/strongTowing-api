namespace StrongTowing.Core.Entities;

public class InvoiceImage
{
    public int Id { get; set; }

    /// <summary>Public path under wwwroot, e.g. /uploads/invoice-images/{invoiceId}/{file}.jpg</summary>
    public string ImageUrl { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
}
