namespace StrongTowing.Core.Entities;

public class WorkspaceTeam
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedBy { get; set; }

    public ICollection<WorkspaceTeamMember> Members { get; set; } = new List<WorkspaceTeamMember>();
    public ICollection<TaskBoard> Boards { get; set; } = new List<TaskBoard>();
}
