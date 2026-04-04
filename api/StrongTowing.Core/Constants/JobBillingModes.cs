namespace StrongTowing.Core.Constants;

/// <summary>How the job total is expected to be covered (billing / payroll).</summary>
public static class JobBillingModes
{
    /// <summary>Card, payment link, or standard cash — existing Stripe/cash flows.</summary>
    public const string Standard = "Standard";

    /// <summary>100% billed to / paid by insurance.</summary>
    public const string InsuranceFull = "InsuranceFull";

    /// <summary>Insurance covers part of the total; client pays the remainder.</summary>
    public const string SplitInsuranceClient = "SplitInsuranceClient";

    /// <summary>Client pays cash to the driver; amount is withheld from driver payroll.</summary>
    public const string CashToDriverPayroll = "CashToDriverPayroll";
}
