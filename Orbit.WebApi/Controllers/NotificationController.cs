using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbit.WebApi.Constants;
using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Helpers;
using Orbit.Application.Models.Responses;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Services;
using Orbit.WebApi.Helpers;

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
    [ProducesResponseType<GenericResponse<PagedResult<NotificationDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<PagedResult<NotificationDto>>> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<PagedResult<NotificationDto>>(default, message: ResponseMessages.InvalidToken));

        var result = await _notificationService.GetNotificationsAsync(profileId.Value, page, Math.Clamp(pageSize, 1, 100));
        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }

    [HttpGet("api/notifications/unread-count")]
    [EndpointSummary("Contar no leídas")]
    [EndpointDescription("Obtiene la cantidad de notificaciones no leídas del usuario autenticado.")]
    [ProducesResponseType<GenericResponse<int>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<int>> GetUnreadCount()
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<int>(default, message: ResponseMessages.InvalidToken));

        var result = await _notificationService.GetUnreadCountAsync(profileId.Value);
        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data, message: result.Message));
    }

    [HttpPatch("api/notifications/{id:guid}/read")]
    [EndpointSummary("Marcar como leída")]
    [EndpointDescription("Marca una notificación específica como leída.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<string>> MarkAsRead(Guid id)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken));

        var result = await _notificationService.MarkAsReadAsync(profileId.Value, id);

        if (!result.IsSuccess)
            return ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }

    [HttpPatch("api/notifications/read-all")]
    [EndpointSummary("Marcar todas como leídas")]
    [EndpointDescription("Marca todas las notificaciones del usuario autenticado como leídas.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<string>> MarkAllAsRead()
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken));

        var result = await _notificationService.MarkAllAsReadAsync(profileId.Value);
        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }
}
