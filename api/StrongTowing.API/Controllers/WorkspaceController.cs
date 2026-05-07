using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Entities;
using StrongTowing.Core.Enums;
using StrongTowing.Infrastructure.Data;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/workspace")]
[Authorize(Roles = $"{UserRoles.SuperAdmin},{UserRoles.Administrator},{UserRoles.Dispatcher}")]
public class WorkspaceController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<WorkspaceController> _logger;

    private static readonly HashSet<string> InternalRoleIds = new(StringComparer.Ordinal)
    {
        UserRoles.RoleIds[UserRoles.SuperAdmin],
        UserRoles.RoleIds[UserRoles.Administrator],
        UserRoles.RoleIds[UserRoles.Dispatcher],
        UserRoles.RoleIds[UserRoles.Driver]
    };

    public WorkspaceController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<WorkspaceController> logger)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    private async Task<string?> GetCurrentUserIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user?.Id;
    }

    private async Task<string> GetRoleNameFromRoleIdAsync(string roleId)
    {
        if (string.IsNullOrEmpty(roleId)) return string.Empty;
        var role = await _roleManager.FindByIdAsync(roleId);
        return role?.Name ?? string.Empty;
    }

    private static bool IsInternalStaffRoleId(string? roleId) =>
        !string.IsNullOrEmpty(roleId) && InternalRoleIds.Contains(roleId);

    private async Task<bool> CanAccessBoardAsync(string userId, int boardId)
    {
        if (await _db.TaskBoardMembers.AnyAsync(m => m.BoardId == boardId && m.UserId == userId))
            return true;
        if (User.IsInRole(UserRoles.SuperAdmin) || User.IsInRole(UserRoles.Administrator) ||
            User.IsInRole(UserRoles.Dispatcher))
            return true;
        return false;
    }

    // --- Eligible users (for pickers) ---

    [HttpGet("eligible-users")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetEligibleUsers(
        [FromQuery] string? search = null,
        [FromQuery] int take = 200)
    {
        take = Math.Clamp(take, 1, 500);
        var q = _userManager.Users.AsQueryable()
            .Where(u => u.IsActive && InternalRoleIds.Contains(u.RoleId));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(s)) ||
                (u.FullName != null && u.FullName.ToLower().Contains(s)));
        }

        var users = await q.OrderBy(u => u.FullName).Take(take).ToListAsync();
        var list = new List<UserDto>();
        foreach (var u in users)
        {
            var roleName = await GetRoleNameFromRoleIdAsync(u.RoleId);
            list.Add(new UserDto
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                FullName = u.FullName,
                PhoneNumber = u.PhoneNumber,
                Role = roleName,
                RoleId = u.RoleId,
                IsActive = u.IsActive,
                HasChangedPassword = u.HasChangedPassword,
                PasswordChangedAt = u.PasswordChangedAt,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                IsAvailableForDispatch = u.IsAvailableForDispatch
            });
        }

        return Ok(list);
    }

    // --- Company employees ---

    [HttpGet("employees")]
    public async Task<ActionResult<IReadOnlyList<CompanyEmployeeDto>>> GetEmployees()
    {
        var rows = await _db.CompanyEmployees
            .AsNoTracking()
            .Include(e => e.User)
            .OrderBy(e => e.User.FullName)
            .ToListAsync();
        var result = new List<CompanyEmployeeDto>();
        foreach (var e in rows)
        {
            var roleName = await GetRoleNameFromRoleIdAsync(e.User.RoleId);
            result.Add(new CompanyEmployeeDto
            {
                Id = e.Id,
                UserId = e.UserId,
                Email = e.User.Email ?? string.Empty,
                FullName = e.User.FullName,
                PhoneNumber = e.User.PhoneNumber,
                Role = roleName,
                JobTitle = e.JobTitle,
                Department = e.Department,
                CreatedAt = e.CreatedAt
            });
        }

        return Ok(result);
    }

    [HttpPost("employees")]
    public async Task<ActionResult<CompanyEmployeeDto>> AddEmployee([FromBody] AddCompanyEmployeeRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null || !user.IsActive)
            return NotFound(new { error = "Not Found", message = "User not found or inactive" });
        if (!IsInternalStaffRoleId(user.RoleId))
            return BadRequest(new { error = "Bad Request", message = "Only internal staff accounts can be added as employees" });
        if (await _db.CompanyEmployees.AnyAsync(e => e.UserId == request.UserId))
            return Conflict(new { error = "Conflict", message = "User is already on the employee roster" });

        var entity = new CompanyEmployee
        {
            UserId = request.UserId,
            JobTitle = request.JobTitle,
            Department = request.Department,
            CreatedAt = DateTime.UtcNow
        };
        _db.CompanyEmployees.Add(entity);
        await _db.SaveChangesAsync();

        var roleName = await GetRoleNameFromRoleIdAsync(user.RoleId);
        var dto = new CompanyEmployeeDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Role = roleName,
            JobTitle = entity.JobTitle,
            Department = entity.Department,
            CreatedAt = entity.CreatedAt
        };
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    [HttpDelete("employees/{userId}")]
    public async Task<IActionResult> RemoveEmployee(string userId)
    {
        var e = await _db.CompanyEmployees.FirstOrDefaultAsync(x => x.UserId == userId);
        if (e == null) return NotFound();
        _db.CompanyEmployees.Remove(e);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // --- Teams ---

    [HttpGet("teams")]
    public async Task<ActionResult<IReadOnlyList<WorkspaceTeamDto>>> GetTeams()
    {
        var teams = await _db.WorkspaceTeams
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new WorkspaceTeamDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                MemberCount = t.Members.Count,
                BoardCount = t.Boards.Count
            })
            .ToListAsync();
        return Ok(teams);
    }

    [HttpPost("teams")]
    public async Task<ActionResult<WorkspaceTeamDto>> CreateTeam([FromBody] CreateWorkspaceTeamRequest request)
    {
        var userId = await GetCurrentUserIdAsync();
        var team = new WorkspaceTeam
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };
        _db.WorkspaceTeams.Add(team);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTeam), new { teamId = team.Id }, new WorkspaceTeamDto
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            CreatedAt = team.CreatedAt,
            MemberCount = 0,
            BoardCount = 0
        });
    }

    [HttpGet("teams/{teamId:int}")]
    public async Task<ActionResult<WorkspaceTeamDto>> GetTeam(int teamId)
    {
        var dto = await _db.WorkspaceTeams
            .AsNoTracking()
            .Where(t => t.Id == teamId)
            .Select(t => new WorkspaceTeamDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                MemberCount = t.Members.Count,
                BoardCount = t.Boards.Count
            })
            .FirstOrDefaultAsync();
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpPut("teams/{teamId:int}")]
    public async Task<IActionResult> UpdateTeam(int teamId, [FromBody] UpdateWorkspaceTeamRequest request)
    {
        var team = await _db.WorkspaceTeams.FirstOrDefaultAsync(t => t.Id == teamId);
        if (team == null) return NotFound();
        team.Name = request.Name.Trim();
        team.Description = request.Description?.Trim();
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("teams/{teamId:int}")]
    public async Task<IActionResult> DeleteTeam(int teamId)
    {
        var team = await _db.WorkspaceTeams.FirstOrDefaultAsync(t => t.Id == teamId);
        if (team == null) return NotFound();
        _db.WorkspaceTeams.Remove(team);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("teams/{teamId:int}/members")]
    public async Task<ActionResult<IReadOnlyList<WorkspaceTeamMemberDto>>> GetTeamMembers(int teamId)
    {
        if (!await _db.WorkspaceTeams.AnyAsync(t => t.Id == teamId)) return NotFound();
        var members = await _db.WorkspaceTeamMembers
            .AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.TeamId == teamId)
            .OrderBy(m => m.User.FullName)
            .ToListAsync();
        var list = new List<WorkspaceTeamMemberDto>();
        foreach (var m in members)
        {
            var roleName = await GetRoleNameFromRoleIdAsync(m.User.RoleId);
            list.Add(new WorkspaceTeamMemberDto
            {
                UserId = m.UserId,
                Email = m.User.Email ?? string.Empty,
                FullName = m.User.FullName,
                Role = roleName,
                JoinedAt = m.JoinedAt
            });
        }

        return Ok(list);
    }

    [HttpPost("teams/{teamId:int}/members")]
    public async Task<IActionResult> AddTeamMember(int teamId, [FromBody] AddTeamMemberRequest request)
    {
        if (!await _db.WorkspaceTeams.AnyAsync(t => t.Id == teamId)) return NotFound();
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null || !user.IsActive)
            return NotFound(new { error = "Not Found", message = "User not found or inactive" });
        var onRoster = await _db.CompanyEmployees.AnyAsync(e => e.UserId == request.UserId);
        if (!onRoster)
            return BadRequest(new { error = "Bad Request", message = "Add the user to the employee roster first" });
        if (await _db.WorkspaceTeamMembers.AnyAsync(m => m.TeamId == teamId && m.UserId == request.UserId))
            return Conflict(new { error = "Conflict", message = "User is already on this team" });

        _db.WorkspaceTeamMembers.Add(new WorkspaceTeamMember
        {
            TeamId = teamId,
            UserId = request.UserId,
            JoinedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("teams/{teamId:int}/members/{userId}")]
    public async Task<IActionResult> RemoveTeamMember(int teamId, string userId)
    {
        var m = await _db.WorkspaceTeamMembers.FirstOrDefaultAsync(x => x.TeamId == teamId && x.UserId == userId);
        if (m == null) return NotFound();
        _db.WorkspaceTeamMembers.Remove(m);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // --- Boards ---

    [HttpGet("teams/{teamId:int}/boards")]
    public async Task<ActionResult<IReadOnlyList<TaskBoardSummaryDto>>> GetBoards(int teamId)
    {
        if (!await _db.WorkspaceTeams.AnyAsync(t => t.Id == teamId)) return NotFound();
        var boards = await _db.TaskBoards
            .AsNoTracking()
            .Where(b => b.TeamId == teamId)
            .OrderBy(b => b.Name)
            .Select(b => new TaskBoardSummaryDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                CreatedAt = b.CreatedAt,
                MemberCount = b.Members.Count
            })
            .ToListAsync();
        return Ok(boards);
    }

    [HttpPost("teams/{teamId:int}/boards")]
    public async Task<ActionResult<TaskBoardSummaryDto>> CreateBoard(int teamId, [FromBody] CreateTaskBoardRequest request)
    {
        if (!await _db.WorkspaceTeams.AnyAsync(t => t.Id == teamId)) return NotFound();
        var userId = await GetCurrentUserIdAsync();
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var board = new TaskBoard
            {
                TeamId = teamId,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };
            _db.TaskBoards.Add(board);
            await _db.SaveChangesAsync();

            var defaultColumns = new[] { ("To do", 0), ("In progress", 1), ("Done", 2) };
            foreach (var (title, order) in defaultColumns)
            {
                _db.TaskBoardColumns.Add(new TaskBoardColumn
                {
                    BoardId = board.Id,
                    Title = title,
                    SortOrder = order
                });
            }

            await _db.SaveChangesAsync();
            if (!string.IsNullOrEmpty(userId))
            {
                _db.TaskBoardMembers.Add(new TaskBoardMember
                {
                    BoardId = board.Id,
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();
            return CreatedAtAction(nameof(GetBoard), new { boardId = board.Id }, new TaskBoardSummaryDto
            {
                Id = board.Id,
                Name = board.Name,
                Description = board.Description,
                CreatedAt = board.CreatedAt,
                MemberCount = string.IsNullOrEmpty(userId) ? 0 : 1
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "CreateBoard failed");
            throw;
        }
    }

    [HttpGet("boards/{boardId:int}")]
    public async Task<ActionResult<TaskBoardDetailDto>> GetBoard(int boardId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (string.IsNullOrEmpty(userId) || !await CanAccessBoardAsync(userId, boardId))
            return Forbid();

        var board = await _db.TaskBoards
            .AsNoTracking()
            .Include(b => b.Team)
            .FirstOrDefaultAsync(b => b.Id == boardId);
        if (board == null) return NotFound();

        var columns = await _db.TaskBoardColumns
            .AsNoTracking()
            .Where(c => c.BoardId == boardId)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        var tickets = await _db.TaskTickets
            .AsNoTracking()
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .Where(t => t.BoardId == boardId)
            .OrderBy(t => t.ColumnId).ThenBy(t => t.SortOrder).ThenBy(t => t.Id)
            .ToListAsync();

        var colDtos = columns.Select(c => new TaskBoardColumnDto
        {
            Id = c.Id,
            Title = c.Title,
            SortOrder = c.SortOrder,
            Tickets = tickets.Where(t => t.ColumnId == c.Id).Select(t => new TaskTicketDto
            {
                Id = t.Id,
                BoardId = t.BoardId,
                ColumnId = t.ColumnId,
                Title = t.Title,
                Description = t.Description,
                Priority = (int)t.Priority,
                SortOrder = t.SortOrder,
                AssigneeUserId = t.AssigneeUserId,
                AssigneeName = t.Assignee?.FullName,
                CreatedByUserId = t.CreatedByUserId,
                CreatedByName = t.CreatedBy?.FullName,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            }).ToList()
        }).ToList();

        return Ok(new TaskBoardDetailDto
        {
            Id = board.Id,
            TeamId = board.TeamId,
            TeamName = board.Team.Name,
            Name = board.Name,
            Description = board.Description,
            CreatedAt = board.CreatedAt,
            Columns = colDtos
        });
    }

    [HttpDelete("boards/{boardId:int}")]
    public async Task<IActionResult> DeleteBoard(int boardId)
    {
        var board = await _db.TaskBoards.FirstOrDefaultAsync(b => b.Id == boardId);
        if (board == null) return NotFound();
        _db.TaskBoards.Remove(board);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("boards/{boardId:int}/members")]
    public async Task<ActionResult<IReadOnlyList<TaskBoardMemberDto>>> GetBoardMembers(int boardId)
    {
        if (!await _db.TaskBoards.AnyAsync(b => b.Id == boardId)) return NotFound();
        var members = await _db.TaskBoardMembers
            .AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.BoardId == boardId)
            .OrderBy(m => m.User.FullName)
            .ToListAsync();
        var list = new List<TaskBoardMemberDto>();
        foreach (var m in members)
        {
            var roleName = await GetRoleNameFromRoleIdAsync(m.User.RoleId);
            list.Add(new TaskBoardMemberDto
            {
                UserId = m.UserId,
                Email = m.User.Email ?? string.Empty,
                FullName = m.User.FullName,
                Role = roleName,
                JoinedAt = m.JoinedAt
            });
        }

        return Ok(list);
    }

    [HttpPost("boards/{boardId:int}/members")]
    public async Task<IActionResult> AddBoardMember(int boardId, [FromBody] AddBoardMemberRequest request)
    {
        var boardEntity = await _db.TaskBoards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == boardId);
        if (boardEntity == null) return NotFound();
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null || !user.IsActive)
            return NotFound(new { error = "Not Found", message = "User not found or inactive" });
        if (!await _db.WorkspaceTeamMembers.AnyAsync(m =>
                m.TeamId == boardEntity.TeamId && m.UserId == request.UserId))
            return BadRequest(new { error = "Bad Request", message = "User must be a member of the team first" });
        if (await _db.TaskBoardMembers.AnyAsync(m => m.BoardId == boardId && m.UserId == request.UserId))
            return Conflict(new { error = "Conflict", message = "User is already on this board" });

        _db.TaskBoardMembers.Add(new TaskBoardMember
        {
            BoardId = boardId,
            UserId = request.UserId,
            JoinedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("boards/{boardId:int}/members/{userId}")]
    public async Task<IActionResult> RemoveBoardMember(int boardId, string userId)
    {
        var m = await _db.TaskBoardMembers.FirstOrDefaultAsync(x => x.BoardId == boardId && x.UserId == userId);
        if (m == null) return NotFound();
        _db.TaskBoardMembers.Remove(m);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // --- Tickets ---

    [HttpPost("boards/{boardId:int}/tickets")]
    public async Task<ActionResult<TaskTicketDto>> CreateTicket(int boardId, [FromBody] CreateTaskTicketRequest request)
    {
        var userId = await GetCurrentUserIdAsync();
        if (string.IsNullOrEmpty(userId) || !await CanAccessBoardAsync(userId, boardId))
            return Forbid();

        int resolvedColumnId;
        if (!request.ColumnId.HasValue)
        {
            var firstId = await _db.TaskBoardColumns
                .Where(c => c.BoardId == boardId)
                .OrderBy(c => c.SortOrder)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();
            if (!firstId.HasValue)
                return BadRequest(new { error = "Bad Request", message = "Board has no columns" });
            resolvedColumnId = firstId.Value;
        }
        else if (!await _db.TaskBoardColumns.AnyAsync(c => c.Id == request.ColumnId.Value && c.BoardId == boardId))
            return BadRequest(new { error = "Bad Request", message = "Invalid column" });
        else
            resolvedColumnId = request.ColumnId.Value;

        var maxOrder = await _db.TaskTickets
            .Where(t => t.BoardId == boardId && t.ColumnId == resolvedColumnId)
            .MaxAsync(t => (int?)t.SortOrder) ?? -1;

        if (!string.IsNullOrEmpty(request.AssigneeUserId))
        {
            if (!await _db.TaskBoardMembers.AnyAsync(m => m.BoardId == boardId && m.UserId == request.AssigneeUserId))
                return BadRequest(new { error = "Bad Request", message = "Assignee must be a board member" });
        }

        var priority = (TaskTicketPriority)Math.Clamp(request.Priority, 0, 3);
        var ticket = new TaskTicket
        {
            BoardId = boardId,
            ColumnId = resolvedColumnId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Priority = priority,
            SortOrder = maxOrder + 1,
            AssigneeUserId = request.AssigneeUserId,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _db.TaskTickets.Add(ticket);
        await _db.SaveChangesAsync();

        var created = await _db.TaskTickets
            .AsNoTracking()
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .FirstAsync(t => t.Id == ticket.Id);

        return CreatedAtAction(nameof(GetBoard), new { boardId }, new TaskTicketDto
        {
            Id = created.Id,
            BoardId = created.BoardId,
            ColumnId = created.ColumnId,
            Title = created.Title,
            Description = created.Description,
            Priority = (int)created.Priority,
            SortOrder = created.SortOrder,
            AssigneeUserId = created.AssigneeUserId,
            AssigneeName = created.Assignee?.FullName,
            CreatedByUserId = created.CreatedByUserId,
            CreatedByName = created.CreatedBy?.FullName,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt
        });
    }

    [HttpPatch("tickets/{ticketId:int}")]
    public async Task<ActionResult<TaskTicketDto>> UpdateTicket(int ticketId, [FromBody] UpdateTaskTicketRequest request)
    {
        var userId = await GetCurrentUserIdAsync();
        var ticket = await _db.TaskTickets.FirstOrDefaultAsync(t => t.Id == ticketId);
        if (ticket == null) return NotFound();
        if (string.IsNullOrEmpty(userId) || !await CanAccessBoardAsync(userId, ticket.BoardId))
            return Forbid();

        if (request.Title != null) ticket.Title = request.Title.Trim();
        if (request.Description != null) ticket.Description = request.Description.Trim();
        if (request.Priority.HasValue)
            ticket.Priority = (TaskTicketPriority)Math.Clamp(request.Priority.Value, 0, 3);

        if (request.AssigneeUserId != null)
        {
            if (request.AssigneeUserId == "")
                ticket.AssigneeUserId = null;
            else if (!await _db.TaskBoardMembers.AnyAsync(m => m.BoardId == ticket.BoardId && m.UserId == request.AssigneeUserId))
                return BadRequest(new { error = "Bad Request", message = "Assignee must be a board member" });
            else
                ticket.AssigneeUserId = request.AssigneeUserId;
        }

        if (request.ColumnId.HasValue)
        {
            if (!await _db.TaskBoardColumns.AnyAsync(c => c.Id == request.ColumnId.Value && c.BoardId == ticket.BoardId))
                return BadRequest(new { error = "Bad Request", message = "Invalid column" });
            ticket.ColumnId = request.ColumnId.Value;
        }

        if (request.SortOrder.HasValue) ticket.SortOrder = request.SortOrder.Value;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var updated = await _db.TaskTickets
            .AsNoTracking()
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .FirstAsync(t => t.Id == ticketId);

        return Ok(new TaskTicketDto
        {
            Id = updated.Id,
            BoardId = updated.BoardId,
            ColumnId = updated.ColumnId,
            Title = updated.Title,
            Description = updated.Description,
            Priority = (int)updated.Priority,
            SortOrder = updated.SortOrder,
            AssigneeUserId = updated.AssigneeUserId,
            AssigneeName = updated.Assignee?.FullName,
            CreatedByUserId = updated.CreatedByUserId,
            CreatedByName = updated.CreatedBy?.FullName,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt
        });
    }

    [HttpDelete("tickets/{ticketId:int}")]
    public async Task<IActionResult> DeleteTicket(int ticketId)
    {
        var userId = await GetCurrentUserIdAsync();
        var ticket = await _db.TaskTickets.FirstOrDefaultAsync(t => t.Id == ticketId);
        if (ticket == null) return NotFound();
        if (string.IsNullOrEmpty(userId) || !await CanAccessBoardAsync(userId, ticket.BoardId))
            return Forbid();
        _db.TaskTickets.Remove(ticket);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
