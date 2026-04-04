namespace StrongTowing.Application.DTOs.Responses;

public class DriverEarningsSummaryDto
{
    public int CompletedJobsCount { get; set; }
    public decimal CompletedJobsTotalRevenue { get; set; }
    public List<DriverPayrollListItemDto> Payrolls { get; set; } = new();

    /// <summary>Jobs where the client paid cash to you (cash-to-driver / payroll deduction flow).</summary>
    public int CashCollectionJobsCount { get; set; }

    /// <summary>Total cash you collected for the company on those jobs.</summary>
    public decimal TotalCashCollected { get; set; }

    /// <summary>Total amount scheduled to be withheld from your payroll (usually matches cash collected).</summary>
    public decimal TotalPayrollDeductionFromCash { get; set; }

    public List<DriverCashCollectionItemDto> RecentCashCollections { get; set; } = new();
}

public class DriverCashCollectionItemDto
{
    public int JobId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal CashCollected { get; set; }
    public decimal PayrollDeductionAmount { get; set; }
    public bool PayrollDeductionRecorded { get; set; }
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
