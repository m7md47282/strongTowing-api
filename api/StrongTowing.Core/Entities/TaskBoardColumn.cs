namespace StrongTowing.Core.Entities;

public class TaskBoardColumn
{
    public int Id { get; set; }
    public int BoardId { get; set; }
    public TaskBoard Board { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public ICollection<TaskTicket> Tickets { get; set; } = new List<TaskTicket>();
}
