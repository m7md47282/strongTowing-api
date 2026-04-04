namespace StrongTowing.Application.DTOs.Responses;

/// <summary>Driver payroll snapshot for admin reports (list/detail).</summary>
public class DriverPayrollAdminDto
{
    public int Id { get; set; }
    public string DriverId { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string? DriverEmail { get; set; }

    public DateTime PayPeriodStart { get; set; }
    public DateTime PayPeriodEnd { get; set; }

    public int TotalJobs { get; set; }
    public int TotalJobMinutes { get; set; }
    public decimal TotalJobRevenue { get; set; }
    public decimal CommissionPercentage { get; set; }
    public decimal GrossEarnings { get; set; }
    public decimal CashCollections { get; set; }
    public decimal NetPay { get; set; }

    public string Status { get; set; } = string.Empty;
    public DateTime? FinalizedAt { get; set; }
    public string? FinalizedBy { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PaidBy { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Result of generate/refresh for a pay period.</summary>
public class GenerateDriverPayrollResponseDto
{
    public DateTime PayPeriodStart { get; set; }
    public DateTime PayPeriodEnd { get; set; }
    public int RowsUpserted { get; set; }
    public List<DriverPayrollAdminDto> Rows { get; set; } = new();
}
