namespace StrongTowing.Application.DTOs.Requests;

/// <summary>
/// Request to generate or refresh draft payroll rows for all drivers with completed jobs in the period.
/// Dates use UTC calendar days: jobs with CompletedAt in [PayPeriodStart, PayPeriodEnd] inclusive are included.
/// </summary>
public class GenerateDriverPayrollRequest
{
    public DateTime PayPeriodStart { get; set; }
    public DateTime PayPeriodEnd { get; set; }
}
