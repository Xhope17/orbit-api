using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Helpers;
using Orbit.Application.Models.Responses;
using Orbit.Application.Interfaces.Services;
using Orbit.WebApi.Helpers;

namespace Orbit.WebApi.Controllers;

[Authorize(Policy = "ModeratorOrAdmin")]
[ApiController]
[EndpointGroupName("Moderator")]
public class ModeratorController : BaseController
{
    private readonly IProfileService _profileService;

    public ModeratorController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpPost("api/moderator/bans")]
    [EndpointSummary("Banear usuario")]
    [EndpointDescription("Banea a un usuario de la plataforma. No puede iniciar sesión. Solo para moderadores y administradores.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<GenericResponse<string>> BanUser([FromBody] ModeratorActionRequest request)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken, isSuccess: false));

        var result = await _profileService.BanUserAsync(profileId.Value, request.Username);

        if (!result.IsSuccess)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message, isSuccess: false));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }

    [HttpDelete("api/moderator/bans/{username}")]
    [EndpointSummary("Desbanear usuario")]
    [EndpointDescription("Remueve el ban de un usuario de la plataforma. Solo para moderadores y administradores.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<GenericResponse<string>> UnbanUser(string username)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken, isSuccess: false));

        var result = await _profileService.UnbanUserAsync(profileId.Value, username);

        if (!result.IsSuccess)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message, isSuccess: false));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }
}

public record ModeratorActionRequest(string Username);
