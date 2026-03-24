using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StrongTowing.API.Services;
using StrongTowing.Application.DTOs.Requests;
using StrongTowing.Application.DTOs.Responses;
using StrongTowing.Core.Constants;
using StrongTowing.Core.Entities;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    private readonly IGoogleRoutesService _googleRoutesService;
    private readonly IOfficeLocationResolver _officeLocationResolver;
    private readonly UserManager<ApplicationUser> _userManager;

    public LocationController(
        IGoogleRoutesService googleRoutesService,
        IOfficeLocationResolver officeLocationResolver,
        UserManager<ApplicationUser> userManager)
    {
        _googleRoutesService = googleRoutesService;
        _officeLocationResolver = officeLocationResolver;
        _userManager = userManager;
    }

    /// <summary>
    /// Office coordinates for map marker (database when set, else appsettings).
    /// </summary>
    [HttpGet("office")]
    [AllowAnonymous]
    public async Task<ActionResult<OfficeLocationDto>> GetOfficeLocation(CancellationToken cancellationToken)
    {
        var (lat, lng) = await _officeLocationResolver.GetOfficeCoordinatesAsync(cancellationToken);
        return Ok(new OfficeLocationDto
        {
            Lat = lat,
            Lng = lng
        });
    }

    /// <summary>
    /// Driving distance and duration from office to client (Google Routes API, server-side).
    /// </summary>
    [HttpPost("calculate-distance")]
    [AllowAnonymous]
    public async Task<ActionResult<RouteResponseDto>> CalculateDistance(
        [FromBody] RouteRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
        {
            return BadRequest(new { error = "Invalid coordinates", message = "Latitude must be between -90 and 90, longitude between -180 and 180." });
        }

        var result = await _googleRoutesService.ComputeRouteAsync(request, cancellationToken);
        if (result == null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "Route unavailable",
                message = "Could not compute route. Check server configuration and Google Routes API."
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Driver: report current GPS (stored on your user for dispatch visibility).
    /// </summary>
    [HttpPost("driver/ping")]
    [Authorize(Roles = UserRoles.Driver)]
    public async Task<IActionResult> DriverPing([FromBody] DriverLocationPingRequest request)
    {
        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
        {
            return BadRequest(new { error = "Invalid coordinates", message = "Latitude must be between -90 and 90, longitude between -180 and 180." });
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return Unauthorized(new { error = "Unauthorized", message = "User not found or inactive." });
        }

        user.LastKnownLatitude = request.Latitude;
        user.LastKnownLongitude = request.Longitude;
        user.LastLocationUtc = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return NoContent();
    }
}
