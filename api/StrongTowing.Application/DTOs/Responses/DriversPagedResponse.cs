namespace StrongTowing.Application.DTOs.Responses;

/// <summary>
/// Drivers list page with assignment breakdown (counts respect search / availability filters but not assignment filter).
/// </summary>
public class DriversPagedResponse : PagedResponse<UserDto>
{
    /// <summary>Drivers matching filters who currently have a non-terminal job.</summary>
    public int DriversWithActiveJobCount { get; set; }

    /// <summary>Drivers matching filters who have no active (non-terminal) job.</summary>
    public int DriversWithoutActiveJobCount { get; set; }
}
