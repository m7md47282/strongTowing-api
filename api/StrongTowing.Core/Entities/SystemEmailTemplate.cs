namespace StrongTowing.Core.Entities;

/// <summary>Per-event custom email content (optional row; missing row uses code defaults).</summary>
public class SystemEmailTemplate
{
    public int Id { get; set; }

    /// <summary>Stable key, e.g. DriverJobAssigned. Unique.</summary>
    public string EventKey { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>When null, plain text is derived from HTML at send time.</summary>
    public string? TextBody { get; set; }
}
