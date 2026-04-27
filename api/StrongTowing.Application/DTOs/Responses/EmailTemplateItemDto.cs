namespace StrongTowing.Application.DTOs.Responses;

public sealed class EmailTemplateItemDto
{
    public string EventKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IReadOnlyList<string> Placeholders { get; set; } = Array.Empty<string>();
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? TextBody { get; set; }
    public bool IsCustom { get; set; }
}
