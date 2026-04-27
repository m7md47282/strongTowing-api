namespace StrongTowing.Application.DTOs.Requests;

public sealed class UpdateEmailTemplatesRequest
{
    public List<EmailTemplateItemUpdate> Items { get; set; } = new();
}

public sealed class EmailTemplateItemUpdate
{
    public string EventKey { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? TextBody { get; set; }
}
