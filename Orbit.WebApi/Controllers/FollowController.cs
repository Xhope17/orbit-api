using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Models.Responses;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.Exceptions;
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
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _followService.FollowUserAsync(profileId, username);
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _followService.UnfollowUserAsync(profileId, username);
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var rsp = await _followService.GetFollowersAsync(username, currentProfileId, page, Math.Clamp(pageSize, 1, 100));
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var rsp = await _followService.GetFollowingAsync(username, currentProfileId, page, Math.Clamp(pageSize, 1, 100));
        return ResponseStatus.Ok(HttpContext, rsp);
    }
}
