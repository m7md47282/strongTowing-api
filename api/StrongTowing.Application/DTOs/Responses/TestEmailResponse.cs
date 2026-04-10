namespace StrongTowing.Application.DTOs.Responses;

public class TestEmailResponse
{
    public bool Success { get; set; }
    public string? ToEmail { get; set; }
    public string? PostmarkMessageId { get; set; }
    public string? ErrorMessage { get; set; }
}
