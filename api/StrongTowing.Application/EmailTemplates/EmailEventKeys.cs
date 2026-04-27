namespace StrongTowing.Application.EmailTemplates;

/// <summary>Stable keys for <see cref="Core.Entities.SystemEmailTemplate"/> and default catalog.</summary>
public static class EmailEventKeys
{
    public const string DriverJobAssigned = "DriverJobAssigned";
    public const string DriverJobCompleted = "DriverJobCompleted";
    public const string DriverPayrollPaid = "DriverPayrollPaid";

    public const string ClientJobCreated = "ClientJobCreated";
    public const string ClientFraudUnderReview = "ClientFraudUnderReview";
    public const string ClientDriverAssigned = "ClientDriverAssigned";
    public const string ClientStatusOnRoute = "ClientStatusOnRoute";
    public const string ClientStatusOnScene = "ClientStatusOnScene";
    public const string ClientStatusLoaded = "ClientStatusLoaded";
    public const string ClientPaymentLinkCreated = "ClientPaymentLinkCreated";
    public const string ClientPaymentSucceeded = "ClientPaymentSucceeded";
    public const string ClientPaymentFailed = "ClientPaymentFailed";
    public const string ClientJobCancelled = "ClientJobCancelled";
    public const string ClientJobCompleted = "ClientJobCompleted";

    public static IReadOnlyList<string> All { get; } =
    [
        DriverJobAssigned,
        DriverJobCompleted,
        DriverPayrollPaid,
        ClientJobCreated,
        ClientFraudUnderReview,
        ClientDriverAssigned,
        ClientStatusOnRoute,
        ClientStatusOnScene,
        ClientStatusLoaded,
        ClientPaymentLinkCreated,
        ClientPaymentSucceeded,
        ClientPaymentFailed,
        ClientJobCancelled,
        ClientJobCompleted
    ];
}
