namespace StrongTowing.Application.DTOs.Requests;

public class TestEmailRequest
{
    public string ToEmail { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? HtmlBody { get; set; }
}
