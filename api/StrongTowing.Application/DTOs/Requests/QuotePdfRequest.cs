namespace StrongTowing.Application.DTOs.Requests;

/// <summary>Payload from the Angular quote print sheet — mirrors <c>QuotePrintData</c>.</summary>
public sealed class QuotePdfRequest
{
    public string QuoteRef { get; set; } = "";
    public string QuoteDateDisplay { get; set; } = "";
    public string ValidUntilDisplay { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string CompanyAddress { get; set; } = "";
    public string CompanyPhone { get; set; } = "";
    public string CompanyEmail { get; set; } = "";
    public string CompanyWebsite { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string ClientPhone { get; set; } = "";
    public string ClientDisplayLine { get; set; } = "";
    public string ServiceType { get; set; } = "";
    public string ServiceDateDisplay { get; set; } = "";
    public string TruckTypeLabel { get; set; } = "";
    public string Pickup { get; set; } = "";
    public string Destination { get; set; } = "";
    public string LoadedMileageDisplay { get; set; } = "";
    public List<QuotePdfLineItemDto> LineItems { get; set; } = new();
    public string TotalsSubtotal { get; set; } = "";
    public string TotalsTaxLabel { get; set; } = "";
    public string TotalsTax { get; set; } = "";
    public string TaxPercent { get; set; } = "";
    public string TotalAmount { get; set; } = "";
    public string NotesPreview { get; set; } = "";
}

public sealed class QuotePdfLineItemDto
{
    public string Description { get; set; } = "";
    public string? Subtitle { get; set; }
    public string Quantity { get; set; } = "";
    public string UnitPrice { get; set; } = "";
    public string Amount { get; set; } = "";
}
