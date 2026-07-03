using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbit.WebApi.Constants;
using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Services;

namespace Orbit.WebApi.Controllers;

[ApiController]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private Guid? GetProfileId()
    {
        var profileIdClaim = User.FindFirst(ClaimConstants.ProfileId)?.Value;
        if (profileIdClaim is null || !Guid.TryParse(profileIdClaim, out var profileId))
            return null;
        return profileId;
    }

    [HttpGet("api/notifications")]
    [EndpointSummary("Obtener notificaciones")]
    [EndpointDescription("Obtiene el historial paginado de notificaciones del usuario autenticado.")]
    [ProducesResponseType<Result<PagedResult<NotificationResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return Unauthorized(new { isSuccess = false, message = ResponseMessages.InvalidToken });

        var result = await _notificationService.GetNotificationsAsync(profileId.Value, page, Math.Clamp(pageSize, 1, 100));
        return Ok(new { isSuccess = true, data = result.Data });
    }

    [HttpGet("api/notifications/unread-count")]
    [EndpointSummary("Contar no leídas")]
    [EndpointDescription("Obtiene la cantidad de notificaciones no leídas del usuario autenticado.")]
    [ProducesResponseType<Result<int>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return Unauthorized(new { isSuccess = false, message = ResponseMessages.InvalidToken });

        var result = await _notificationService.GetUnreadCountAsync(profileId.Value);
        return Ok(new { isSuccess = true, data = result.Data });
    }

    [HttpPatch("api/notifications/{id:guid}/read")]
    [EndpointSummary("Marcar como leída")]
    [EndpointDescription("Marca una notificación específica como leída.")]
    [ProducesResponseType<Result>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return Unauthorized(new { isSuccess = false, message = ResponseMessages.InvalidToken });

        var result = await _notificationService.MarkAsReadAsync(profileId.Value, id);

        if (!result.IsSuccess)
            return NotFound(new { isSuccess = false, message = result.Message });

        return Ok(new { isSuccess = true, message = result.Message });
    }

    [HttpPatch("api/notifications/read-all")]
    [EndpointSummary("Marcar todas como leídas")]
    [EndpointDescription("Marca todas las notificaciones del usuario autenticado como leídas.")]
    [ProducesResponseType<Result>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return Unauthorized(new { isSuccess = false, message = ResponseMessages.InvalidToken });

        var result = await _notificationService.MarkAllAsReadAsync(profileId.Value);
        return Ok(new { isSuccess = true, message = result.Message });
    }
}
