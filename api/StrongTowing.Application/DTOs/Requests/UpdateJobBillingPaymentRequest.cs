using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

/// <summary>Dispatcher/admin: set how the job is paid (insurance, split, cash-to-driver payroll).</summary>
public class UpdateJobBillingPaymentRequest
{
    [Required]
    [MaxLength(64)]
    public string BillingPaymentMode { get; set; } = string.Empty;

    public decimal? InsuranceCoveredAmount { get; set; }
    public decimal? ClientCoveredAmount { get; set; }
    public bool InsurancePortionBilled { get; set; }
    public bool ClientPortionPaid { get; set; }
    public decimal? DriverCashCollectedAmount { get; set; }
    public decimal? PayrollDeductionAmount { get; set; }
    public bool PayrollDeductionRecorded { get; set; }
}
