namespace StrongTowing.Core.Entities;

/// <summary>
/// Staff roster entry linking an internal user account to HR-style fields.
/// </summary>
public class CompanyEmployee
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
