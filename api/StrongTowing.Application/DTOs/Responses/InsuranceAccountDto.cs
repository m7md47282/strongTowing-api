namespace StrongTowing.Application.DTOs.Responses;

public class InsuranceAccountDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public string? ContactName { get; set; }
    public string? BillingEmail { get; set; }
    public string? BillingPhone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public decimal RateAB { get; set; }
    public decimal RateBC { get; set; }
    public decimal RateCA { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
