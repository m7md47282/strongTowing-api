namespace StrongTowing.Application.DTOs.Responses;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string? RefreshToken { get; set; } // Nullable for backward compatibility
    public UserDto User { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}


