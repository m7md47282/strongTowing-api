namespace StrongTowing.Application.EmailTemplates;

public sealed class EmailTemplateDefinition
{
    public required string EventKey { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<string> Placeholders { get; init; }
    public required string DefaultSubject { get; init; }
    public required string DefaultInnerHtml { get; init; }
}

public static class EmailTemplateDefinitions
{
    public static IReadOnlyList<EmailTemplateDefinition> All { get; } =
    [
        new EmailTemplateDefinition
        {
            EventKey = EmailEventKeys.DriverJobAssigned,
            DisplayName = "Driver — new job assigned",
            Description = "Sent to the driver when a job is assigned to them.",
            Placeholders = ["JobId", "PickupSummary"],
            DefaultSubject = "Job #{{JobId}} — new assignment",
            DefaultInnerHtml = """
                <p>Hello,</p>
                <p>A new job has been assigned to you.</p>
                <p><strong>Job #{{JobId}}</strong><br/>Pickup summary: {{PickupSummary}}</p>
                <p>Please open the driver app for full details.</p>
                """
        },
        new EmailTemplateDefinition
        {
            EventKey = EmailEventKeys.DriverJobCompleted,
            DisplayName = "Driver — job completed",
            Description = "Sent to the driver when a job is marked completed.",
            Placeholders = ["JobId"],
            DefaultSubject = "Job #{{JobId}} completed",
            DefaultInnerHtml = """
                <p>Hello,</p>
                <p>Job <strong>#{{JobId}}</strong> has been marked as completed.</p>
                """
        },
        new EmailTemplateDefinition
        {
            EventKey = EmailEventKeys.DriverPayrollPaid,
            DisplayName = "Driver — payroll paid",
            Description = "Sent to the driver when a payroll period is marked paid.",
            Placeholders = ["PayPeriodStart", "PayPeriodEnd", "NetPay"],
            DefaultSubject = "Payroll notification",
            DefaultInnerHtml = """
                <p>Hello,</p>
                <p>Your payroll for the period <strong>{{PayPeriodStart}}</strong> through <strong>{{PayPeriodEnd}}</strong> has been marked as paid.</p>
                <p>Net pay: <strong>{{NetPay}}</strong></p>
                """
        },
        new EmailTemplateDefinition
        {
            EventKey = EmailEventKeys.ClientJobCreated,
            DisplayName = "Client — request received",
            Description = "Sent to the client when a new job request is created.",
            Placeholders = ["JobId"],
            DefaultSubject = "We received your request — Job #{{JobId}}",
            DefaultInnerHtml = """
                <p>Thank you for contacting us.</p>
                <p>We have received your service request. Your reference number is <strong>#{{JobId}}</strong>.</p>
                <p>We will keep you updated as your request is processed.</p>
                """
        },
        new EmailTemplateDefinition
        {
            EventKey = EmailEventKeys.ClientFraudUnderReview,
            DisplayName = "Client — request under review",
            Description = "Sent when a job is held for fraud or risk review.",
            Placeholders = ["JobId"],
            DefaultSubject = "Job #{{JobId}} — under review",
            DefaultInnerHtml = """
                <p>Thank you for your request.</p>
                <p>Job <strong>#{{JobId}}</strong> is currently under review. Our team may contact you shortly.</p>
                """
        },
        new EmailTemplateDefinition
        {
            EventKey = EmailEventKeys.ClientDriverAssigned,
            DisplayName = "Client — driver assigned",
            Description = "Sent when a driver is assigned to the job.",
            Placeholders = ["JobId"],
            DefaultSubject = "A driver is assigned — Job #{{JobId}}",
            DefaultInnerHtml = """
                <p>Good news — a driver has been assigned to job <strong>#{{JobId}}</strong>.</p>
                <p>You will receive further updates as the job progresses.</p>
                """
        },
        new EmailTemplateDefinition
        {
            EventKey = EmailEventKeys.ClientStatusOnRoute,
            DisplayName = "Client — driver en route",
            Description = "Sent when the job status changes to en route.",
            Placeholders = ["JobId"],
            DefaultSubject = "Job #{{JobId}} — driver en route",
            DefaultInnerHtml = """
                <p>Your driver is on the way.</p>
                <p>Job <strong>#{{JobId}}</strong> — status: <strong>en route</strong>.</p>
                """
        },
        new EmailTemplateDefinition
        {
            EventKey = EmailEventKeys.ClientStatusOnScene,
            DisplayName = "Client — driver on scene",
            Description = "Sent when the job status changes to on scene.",
            Placeholders = ["JobId"],
            DefaultSubject = "Job #{{JobId}} — driver on scene",
            DefaultInnerHtml = """
                <p>Your driver has arrived on scene.</p>
                <p>Job <strong>#{{JobId}}</strong> — status: <strong>on scene</strong>.</p>
                """
        },
        new EmailTemplateDefinition
        {
            EventKey = EmailEventKeys.ClientStatusLoaded,
            DisplayName = "Client — vehicle loaded",
            Description = "Sent when the vehicle is loaded.",
            Placeholders = ["JobId"],
            DefaultSubject = "Job #{{JobId}} — vehicle loaded",
            DefaultInnerHtml = """
                <p>Your vehicle has been loaded.</p>
                <p>Job <strong>#{{JobId}}</strong> — status: <strong>loaded</strong>.</p>
                """
        },
        new EmailTemplateDefinition
        {
            EventKey = EmailEventKeys.ClientPaymentLinkCreated,
            DisplayName = "Client — payment link",
            Description = "Sent with a link to pay for a job.",
            Placeholders = ["JobId", "Amount", "LinkUrl"],
            DefaultSubject = "Payment for Job #{{JobId}}",
            DefaultInnerHtml = """
                <p>Please complete payment for job <strong>#{{JobId}}</strong>.</p>
                <p>Amount due: <strong>{{Amount}}</strong></p>
                <p><a href="{{LinkUrl}}" style="color:#1d4ed8;">Pay now</a></p>
                <p style="font-size:13px;color:#6b7280;word-break:break-all;">If the button does not work, copy and paste this link into your browser:<br/>{{LinkUrl}}</p>
                """
        },
        new EmailTemplateDefinition
        {
            EventKey = EmailEventKeys.ClientPaymentSucceeded,
            DisplayName = "Client — payment received",
            Description = "Sent after a successful payment.",
            Placeholders = ["JobId", "AmountLine"],
            DefaultSubject = "Payment received — Job #{{JobId}}",
            DefaultInnerHtml = """
                <p>Thank you.</p>
                <p>We have received your payment{{AmountLine}} for job <strong>#{{JobId}}</strong>.</p>
                """
        },
        new EmailTemplateDefinition
        {
            EventKey = EmailEventKeys.ClientPaymentFailed,
            DisplayName = "Client — payment issue",
            Description = "Sent when a payment attempt fails.",
            Placeholders = ["JobId"],
            DefaultSubject = "Payment issue — Job #{{JobId}}",
            DefaultInnerHtml = """
                <p>We were unable to complete your payment for job <strong>#{{JobId}}</strong>.</p>
                <p>Please try again or contact us for assistance.</p>
                """
        },
        new EmailTemplateDefinition
        {
            EventKey = EmailEventKeys.ClientJobCancelled,
            DisplayName = "Client — job cancelled",
            Description = "Sent when a job is cancelled. FeeSentence is empty or a sentence about cancellation fees.",
            Placeholders = ["JobId", "FeeSentence"],
            DefaultSubject = "Job #{{JobId}} cancelled",
            DefaultInnerHtml = """
                <p>Job <strong>#{{JobId}}</strong> has been cancelled.{{FeeSentence}}</p>
                """
        },
        new EmailTemplateDefinition
        {
            EventKey = EmailEventKeys.ClientJobCompleted,
            DisplayName = "Client — job completed",
            Description = "Sent when the job is completed.",
            Placeholders = ["JobId"],
            DefaultSubject = "Job #{{JobId}} completed",
            DefaultInnerHtml = """
                <p>Your service for job <strong>#{{JobId}}</strong> is complete.</p>
                <p>Thank you for choosing us.</p>
                """
        }
    ];

    public static EmailTemplateDefinition? ByKey(string eventKey) =>
        All.FirstOrDefault(d => string.Equals(d.EventKey, eventKey, StringComparison.Ordinal));
}
