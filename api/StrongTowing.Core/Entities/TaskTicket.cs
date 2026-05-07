using StrongTowing.Core.Enums;

namespace StrongTowing.Core.Entities;

public class TaskTicket
{
    public int Id { get; set; }
    public int BoardId { get; set; }
    public TaskBoard Board { get; set; } = null!;
    public int ColumnId { get; set; }
    public TaskBoardColumn Column { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskTicketPriority Priority { get; set; } = TaskTicketPriority.Normal;
    public int SortOrder { get; set; }
    public string? AssigneeUserId { get; set; }
    public ApplicationUser? Assignee { get; set; }
    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
