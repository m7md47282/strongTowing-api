namespace StrongTowing.Core.Entities;

public class TaskBoard
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public WorkspaceTeam Team { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedBy { get; set; }

    public ICollection<TaskBoardMember> Members { get; set; } = new List<TaskBoardMember>();
    public ICollection<TaskBoardColumn> Columns { get; set; } = new List<TaskBoardColumn>();
    public ICollection<TaskTicket> Tickets { get; set; } = new List<TaskTicket>();
}
