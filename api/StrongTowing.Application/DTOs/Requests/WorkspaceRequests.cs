using System.ComponentModel.DataAnnotations;

namespace StrongTowing.Application.DTOs.Requests;

public class AddCompanyEmployeeRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    [MaxLength(200)]
    public string? JobTitle { get; set; }
    [MaxLength(200)]
    public string? Department { get; set; }
}

public class CreateWorkspaceTeamRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(2000)]
    public string? Description { get; set; }
}

public class UpdateWorkspaceTeamRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(2000)]
    public string? Description { get; set; }
}

public class AddTeamMemberRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;
}

public class CreateTaskBoardRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(2000)]
    public string? Description { get; set; }
}

public class AddBoardMemberRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;
}

public class CreateTaskTicketRequest
{
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    [MaxLength(8000)]
    public string? Description { get; set; }
    public int? ColumnId { get; set; }
    public string? AssigneeUserId { get; set; }
    public int Priority { get; set; } = 1;
}

public class UpdateTaskTicketRequest
{
    [MaxLength(500)]
    public string? Title { get; set; }
    [MaxLength(8000)]
    public string? Description { get; set; }
    public int? ColumnId { get; set; }
    public int? SortOrder { get; set; }
    public string? AssigneeUserId { get; set; }
    public int? Priority { get; set; }
}
