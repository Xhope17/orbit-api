using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbit.WebApi.Models;
using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Models.Responses;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.Exceptions;
using Orbit.WebApi.Helpers;

namespace Orbit.WebApi.Controllers;

public class ProfileController : BaseController
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [AllowAnonymous]
    [HttpGet("api/profiles/{username}")]
    [EndpointSummary("Obtener perfil")]
    [EndpointDescription("Obtiene el perfil público de un usuario por su nombre de usuario.")]
    [ProducesResponseType<GenericResponse<ProfileDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<ProfileDto>> GetByUsername(string username)
    {
        var currentProfileId = GetProfileId();
        var rsp = await _profileService.GetProfileByUsernameAsync(username, currentProfileId);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpPut("api/profile")]
    [EndpointSummary("Actualizar perfil")]
    [EndpointDescription("Actualiza el display name, biografía y privacidad del perfil autenticado.")]
    [ProducesResponseType<GenericResponse<ProfileDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<ProfileDto>> Update([FromBody] UpdateProfileRequest request)
    {
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _profileService.UpdateProfileAsync(authUserId, request.DisplayName, request.Bio, request.IsPrivate);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpPut("api/profile/avatar")]
    [EndpointSummary("Subir avatar")]
    [EndpointDescription("Sube o actualiza la foto de perfil del usuario autenticado.")]
    [ProducesResponseType<GenericResponse<ProfileDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<ProfileDto>> UpdateAvatar(IFormFile file)
    {
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        using var stream = file.OpenReadStream();
        var rsp = await _profileService.UpdateProfilePictureAsync(authUserId, stream, file.FileName);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpDelete("api/profile/avatar")]
    [EndpointSummary("Eliminar avatar")]
    [EndpointDescription("Elimina la foto de perfil del usuario autenticado.")]
    [ProducesResponseType<GenericResponse<ProfileDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<ProfileDto>> RemoveAvatar()
    {
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _profileService.RemoveProfilePictureAsync(authUserId);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpPut("api/profile/banner")]
    [EndpointSummary("Subir banner")]
    [EndpointDescription("Sube o actualiza el banner del perfil del usuario autenticado.")]
    [ProducesResponseType<GenericResponse<ProfileDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<ProfileDto>> UpdateBanner(IFormFile file)
    {
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        using var stream = file.OpenReadStream();
        var rsp = await _profileService.UpdateBannerAsync(authUserId, stream, file.FileName);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpDelete("api/profile/banner")]
    [EndpointSummary("Eliminar banner")]
    [EndpointDescription("Elimina el banner del perfil del usuario autenticado.")]
    [ProducesResponseType<GenericResponse<ProfileDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<ProfileDto>> RemoveBanner()
    {
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _profileService.RemoveBannerAsync(authUserId);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpGet("api/profiles/search")]
    [EndpointSummary("Buscar perfiles")]
    [EndpointDescription("Busca perfiles por nombre de usuario.")]
    [ProducesResponseType<GenericResponse<PagedResult<SearchProfileDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<PagedResult<SearchProfileDto>>> Search(
        [FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var currentProfileId = GetProfileId();
        var rsp = await _profileService.SearchProfilesAsync(q, currentProfileId, page, Math.Clamp(pageSize, 1, 50));
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpPost("api/profiles/{username}/block")]
    [EndpointSummary("Bloquear usuario")]
    [EndpointDescription("Bloquea a un usuario y elimina follows en ambas direcciones.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<string>> BlockUser(string username)
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _profileService.BlockUserAsync(profileId, username);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpDelete("api/profiles/{username}/block")]
    [EndpointSummary("Desbloquear usuario")]
    [EndpointDescription("Desbloquea a un usuario previamente bloqueado.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<string>> UnblockUser(string username)
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _profileService.UnblockUserAsync(profileId, username);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpGet("api/profile/blocked")]
    [EndpointSummary("Usuarios bloqueados")]
    [EndpointDescription("Obtiene la lista paginada de usuarios bloqueados por el perfil autenticado.")]
    [ProducesResponseType<GenericResponse<PagedResult<BlockedUserDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<PagedResult<BlockedUserDto>>> GetBlockedUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _profileService.GetBlockedUsersAsync(profileId, page, Math.Clamp(pageSize, 1, 50));
        return ResponseStatus.Ok(HttpContext, rsp);
    }
}
