namespace StrongTowing.Application.DTOs.Requests;

public class TestSmsRequest
{
    /// <summary>Destination phone (US-style or E.164); normalized server-side.</summary>
    public string ToPhone { get; set; } = string.Empty;

    /// <summary>Optional body; default test message if empty.</summary>
    public string? Message { get; set; }
}
