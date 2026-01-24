using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Core.Entities;

public class RefreshToken
{
    [Key]
    public string Token { get; set; } = string.Empty; // The actual refresh token value
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    
    public bool IsRevoked => RevokedAt != null;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}
