using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StrongTowing.Application.Abstractions;
using StrongTowing.Application.DTOs.Requests;

namespace StrongTowing.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IFcmNotificationService _fcm;

    public NotificationsController(IFcmNotificationService fcm)
    {
        _fcm = fcm;
    }

    /// <summary>Register this browser's FCM token. The server is the only component that sends notifications.</summary>
    [HttpPost("fcm/register")]
    public async Task<IActionResult> RegisterFcmToken([FromBody] RegisterFcmTokenRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _fcm.RegisterTokenAsync(userId, request.Token, cancellationToken);
        return NoContent();
    }

    /// <summary>Remove an FCM token (e.g. on logout).</summary>
    [HttpPost("fcm/unregister")]
    public async Task<IActionResult> UnregisterFcmToken([FromBody] RegisterFcmTokenRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _fcm.UnregisterTokenAsync(userId, request.Token, cancellationToken);
        return NoContent();
    }
}
