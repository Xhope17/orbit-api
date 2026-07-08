using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Helpers;
using Orbit.Application.Models.Responses;
using Orbit.Application.Interfaces.Services;
using Orbit.WebApi.Helpers;

namespace Orbit.WebApi.Controllers;

[Authorize(Policy = "AdminOnly")]
[ApiController]
[EndpointGroupName("Admin")]
public class AdminController : BaseController
{
    private readonly IRoleService _roleService;

    public AdminController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpPost("api/admin/moderators")]
    [EndpointSummary("Asignar moderador")]
    [EndpointDescription("Asigna el rol de moderador a un usuario. Solo para administradores.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<GenericResponse<string>> AssignModerator([FromBody] ModeratorRequest request)
    {
        var adminProfileId = GetProfileId();
        if (adminProfileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken, isSuccess: false));

        var result = await _roleService.AssignModeratorAsync(adminProfileId.Value, request.Username);

        if (!result.IsSuccess)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message, isSuccess: false));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }

    [HttpDelete("api/admin/moderators/{username}")]
    [EndpointSummary("Remover moderador")]
    [EndpointDescription("Remueve el rol de moderador de un usuario. Solo para administradores.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<GenericResponse<string>> RemoveModerator(string username)
    {
        var adminProfileId = GetProfileId();
        if (adminProfileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken, isSuccess: false));

        var result = await _roleService.RemoveModeratorAsync(adminProfileId.Value, username);

        if (!result.IsSuccess)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message, isSuccess: false));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }
}

public record ModeratorRequest(string Username);
