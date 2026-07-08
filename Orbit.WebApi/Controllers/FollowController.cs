using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Helpers;
using Orbit.Application.Models.Responses;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Services;
using Orbit.WebApi.Helpers;

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
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<string>> Follow(string username)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken));

        var result = await _followService.FollowUserAsync(profileId.Value, username);

        if (!result.IsSuccess)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }

    [Authorize]
    [HttpDelete("api/profiles/{username}/follow")]
    [EndpointSummary("Dejar de seguir")]
    [EndpointDescription("Deja de seguir a un usuario por su nombre de usuario.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<string>> Unfollow(string username)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken));

        var result = await _followService.UnfollowUserAsync(profileId.Value, username);

        if (!result.IsSuccess)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }

    [AllowAnonymous]
    [HttpGet("api/profiles/{username}/followers")]
    [EndpointSummary("Obtener seguidores")]
    [EndpointDescription("Obtiene la lista de seguidores de un perfil.")]
    [ProducesResponseType<GenericResponse<PagedResult<PostAuthorDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<PagedResult<PostAuthorDto>>> GetFollowers(string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var currentProfileId = GetProfileId();
        var result = await _followService.GetFollowersAsync(username, currentProfileId, page, Math.Clamp(pageSize, 1, 100));

        if (!result.IsSuccess)
            return ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<PagedResult<PostAuthorDto>>(default, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }

    [AllowAnonymous]
    [HttpGet("api/profiles/{username}/following")]
    [EndpointSummary("Obtener seguidos")]
    [EndpointDescription("Obtiene la lista de usuarios que sigue un perfil.")]
    [ProducesResponseType<GenericResponse<PagedResult<PostAuthorDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<PagedResult<PostAuthorDto>>> GetFollowing(string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var currentProfileId = GetProfileId();
        var result = await _followService.GetFollowingAsync(username, currentProfileId, page, Math.Clamp(pageSize, 1, 100));

        if (!result.IsSuccess)
            return ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<PagedResult<PostAuthorDto>>(default, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }
}
