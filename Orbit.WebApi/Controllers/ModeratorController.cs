using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Interfaces.Services;

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
    [ProducesResponseType<Result>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> BanUser([FromBody] ModeratorActionRequest request)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return Unauthorized(new { isSuccess = false, message = ResponseMessages.InvalidToken });

        var result = await _profileService.BanUserAsync(profileId.Value, request.Username);

        if (!result.IsSuccess)
            return BadRequest(new { isSuccess = false, message = result.Message });

        return Ok(new { isSuccess = true, message = result.Message });
    }

    [HttpDelete("api/moderator/bans/{username}")]
    [EndpointSummary("Desbanear usuario")]
    [EndpointDescription("Remueve el ban de un usuario de la plataforma. Solo para moderadores y administradores.")]
    [ProducesResponseType<Result>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnbanUser(string username)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return Unauthorized(new { isSuccess = false, message = ResponseMessages.InvalidToken });

        var result = await _profileService.UnbanUserAsync(profileId.Value, username);

        if (!result.IsSuccess)
            return BadRequest(new { isSuccess = false, message = result.Message });

        return Ok(new { isSuccess = true, message = result.Message });
    }
}

public record ModeratorActionRequest(string Username);
