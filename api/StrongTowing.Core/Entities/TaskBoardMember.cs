namespace StrongTowing.Core.Entities;

public class TaskBoardMember
{
    public int BoardId { get; set; }
    public TaskBoard Board { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
