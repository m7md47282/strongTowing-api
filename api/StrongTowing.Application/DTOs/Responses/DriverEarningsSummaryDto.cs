namespace StrongTowing.Application.DTOs.Responses;

public class DriverEarningsSummaryDto
{
    public int CompletedJobsCount { get; set; }
    public decimal CompletedJobsTotalRevenue { get; set; }
    public List<DriverPayrollListItemDto> Payrolls { get; set; } = new();
}

public class DriverPayrollListItemDto
{
    public int Id { get; set; }
    public DateTime PayPeriodStart { get; set; }
    public DateTime PayPeriodEnd { get; set; }
    public int TotalJobs { get; set; }
    public decimal TotalJobRevenue { get; set; }
    public decimal CommissionPercentage { get; set; }
    public decimal GrossEarnings { get; set; }
    public decimal NetPay { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
}
