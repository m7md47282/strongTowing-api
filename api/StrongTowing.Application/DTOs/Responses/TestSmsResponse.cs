namespace StrongTowing.Application.DTOs.Responses;

public class TestSmsResponse
{
    public bool Success { get; set; }
    public string? ToE164 { get; set; }
    public string? TwilioMessageSid { get; set; }
    public string? ErrorMessage { get; set; }
}
