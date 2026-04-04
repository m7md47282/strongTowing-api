namespace StrongTowing.Core.Constants;

/// <summary>
/// Documents how driver payroll snapshots are calculated in this system.
/// These are operational estimates for dispatch/finance workflows — not tax or legal payroll advice.
/// </summary>
public static class PayrollBusinessRules
{
    /// <summary>
    /// Commission is applied to <see cref="CommissionBaseDescription"/> (sum of job bill totals in the period).
    /// </summary>
    public const string CommissionBaseDescription =
        "TotalJobRevenue = sum of Job.Cost for completed jobs in the pay period (UTC date boundaries).";

    /// <summary>
    /// Gross driver earnings before cash withheld for the company.
    /// </summary>
    public const string GrossEarningsFormula =
        "GrossEarnings = TotalJobRevenue × (CommissionPercentage ÷ 100), where CommissionPercentage is copied from SystemSettings at generation time.";

    /// <summary>
    /// Cash the driver collected for the company on cash-to-driver jobs; treated as withheld from net pay.
    /// </summary>
    public const string CashCollectionsFormula =
        "CashCollections = sum over completed jobs in the period with BillingPaymentMode = CashToDriverPayroll of " +
        "(PayrollDeductionAmount ?? DriverCashCollectedAmount ?? 0).";

    /// <summary>
    /// Amount to disburse to the driver for the period (before external tax/withholding).
    /// </summary>
    public const string NetPayFormula =
        "NetPay = GrossEarnings − CashCollections (both non‑negative; NetPay may be negative if cash withheld exceeds gross — review such rows).";

    /// <summary>
    /// Elapsed time from job creation to completion — not legal attendance or overtime hours.
    /// </summary>
    public const string JobTimeDisclaimer =
        "TotalJobMinutes is the sum of (CompletedAt − CreatedAt) in minutes for completed jobs in the period; it is an operational estimate, not certified timeclock hours.";
}
