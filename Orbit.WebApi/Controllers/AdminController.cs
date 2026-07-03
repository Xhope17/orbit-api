using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Interfaces.Services;

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
    [ProducesResponseType<Result>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignModerator([FromBody] ModeratorRequest request)
    {
        var adminProfileId = GetProfileId();
        if (adminProfileId is null)
            return Unauthorized(new { isSuccess = false, message = ResponseMessages.InvalidToken });

        var result = await _roleService.AssignModeratorAsync(adminProfileId.Value, request.Username);

        if (!result.IsSuccess)
            return BadRequest(new { isSuccess = false, message = result.Message });

        return Ok(new { isSuccess = true, message = result.Message });
    }

    [HttpDelete("api/admin/moderators/{username}")]
    [EndpointSummary("Remover moderador")]
    [EndpointDescription("Remueve el rol de moderador de un usuario. Solo para administradores.")]
    [ProducesResponseType<Result>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveModerator(string username)
    {
        var adminProfileId = GetProfileId();
        if (adminProfileId is null)
            return Unauthorized(new { isSuccess = false, message = ResponseMessages.InvalidToken });

        var result = await _roleService.RemoveModeratorAsync(adminProfileId.Value, username);

        if (!result.IsSuccess)
            return BadRequest(new { isSuccess = false, message = result.Message });

        return Ok(new { isSuccess = true, message = result.Message });
    }
}

public record ModeratorRequest(string Username);
