using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Services;

namespace Orbit.WebApi.Controllers;

public class FollowController : BaseController
{
    private readonly IFollowService _followService;

    public FollowController(IFollowService followService)
    {
        _followService = followService;
    }

    [Authorize]
    [HttpPost("api/profiles/{username}/follow")]
    [EndpointSummary("Seguir usuario")]
    [EndpointDescription("Sigue a un usuario por su nombre de usuario.")]
    [ProducesResponseType<Result>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Follow(string username)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return Unauthorized(new { isSuccess = false, message = ResponseMessages.InvalidToken });

        var result = await _followService.FollowUserAsync(profileId.Value, username);

        if (!result.IsSuccess)
            return BadRequest(new { isSuccess = false, message = result.Message });

        return Ok(new { isSuccess = true, message = result.Message });
    }

    [Authorize]
    [HttpDelete("api/profiles/{username}/follow")]
    [EndpointSummary("Dejar de seguir")]
    [EndpointDescription("Deja de seguir a un usuario por su nombre de usuario.")]
    [ProducesResponseType<Result>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Unfollow(string username)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return Unauthorized(new { isSuccess = false, message = ResponseMessages.InvalidToken });

        var result = await _followService.UnfollowUserAsync(profileId.Value, username);

        if (!result.IsSuccess)
            return BadRequest(new { isSuccess = false, message = result.Message });

        return Ok(new { isSuccess = true, message = result.Message });
    }

    [AllowAnonymous]
    [HttpGet("api/profiles/{username}/followers")]
    [EndpointSummary("Obtener seguidores")]
    [EndpointDescription("Obtiene la lista de seguidores de un perfil.")]
    [ProducesResponseType<Result<PagedResult<PostAuthorResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFollowers(string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var currentProfileId = GetProfileId();
        var result = await _followService.GetFollowersAsync(username, currentProfileId, page, Math.Clamp(pageSize, 1, 100));

        if (!result.IsSuccess)
            return NotFound(new { isSuccess = false, message = result.Message });

        return Ok(new { isSuccess = true, data = result.Data });
    }

    [AllowAnonymous]
    [HttpGet("api/profiles/{username}/following")]
    [EndpointSummary("Obtener seguidos")]
    [EndpointDescription("Obtiene la lista de usuarios que sigue un perfil.")]
    [ProducesResponseType<Result<PagedResult<PostAuthorResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFollowing(string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var currentProfileId = GetProfileId();
        var result = await _followService.GetFollowingAsync(username, currentProfileId, page, Math.Clamp(pageSize, 1, 100));

        if (!result.IsSuccess)
            return NotFound(new { isSuccess = false, message = result.Message });

        return Ok(new { isSuccess = true, data = result.Data });
    }

}
