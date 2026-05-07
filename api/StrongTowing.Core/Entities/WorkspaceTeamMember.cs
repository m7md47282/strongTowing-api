namespace StrongTowing.Core.Entities;

public class WorkspaceTeamMember
{
    public int TeamId { get; set; }
    public WorkspaceTeam Team { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
