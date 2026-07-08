using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbit.WebApi.Models;
using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Helpers;
using Orbit.Application.Models.Responses;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Services;
using Orbit.WebApi.Helpers;

namespace Orbit.WebApi.Controllers;

public class ProfileController : BaseController
{
    private readonly IProfileService _profileService;
    private readonly IValidator<UpdateProfileRequest> _updateProfileValidator;

    public ProfileController(
        IProfileService profileService,
        IValidator<UpdateProfileRequest> updateProfileValidator)
    {
        _profileService = profileService;
        _updateProfileValidator = updateProfileValidator;
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
        var result = await _profileService.GetProfileByUsernameAsync(username, currentProfileId);

        if (!result.IsSuccess)
            return ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<ProfileDto>(default, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
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
        var validationResult = await _updateProfileValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<ProfileDto>(default, errors: [.. errors], message: ResponseMessages.ValidationFailed));
        }

        var authUserId = GetAuthUserId();
        if (authUserId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<ProfileDto>(default, message: ResponseMessages.InvalidToken));

        var result = await _profileService.UpdateProfileAsync(authUserId.Value, request.DisplayName, request.Bio, request.IsPrivate);

        if (!result.IsSuccess)
            return ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<ProfileDto>(default, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
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
        if (file is null || file.Length == 0)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<ProfileDto>(default, message: ResponseMessages.FileRequired));

        var authUserId = GetAuthUserId();
        if (authUserId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<ProfileDto>(default, message: ResponseMessages.InvalidToken));

        using var stream = file.OpenReadStream();
        var result = await _profileService.UpdateProfilePictureAsync(authUserId.Value, stream, file.FileName);

        if (!result.IsSuccess)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<ProfileDto>(default, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
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
        var authUserId = GetAuthUserId();
        if (authUserId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<ProfileDto>(default, message: ResponseMessages.InvalidToken));

        var result = await _profileService.RemoveProfilePictureAsync(authUserId.Value);

        if (!result.IsSuccess)
            return ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<ProfileDto>(default, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
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
        if (file is null || file.Length == 0)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<ProfileDto>(default, message: ResponseMessages.FileRequired));

        var authUserId = GetAuthUserId();
        if (authUserId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<ProfileDto>(default, message: ResponseMessages.InvalidToken));

        using var stream = file.OpenReadStream();
        var result = await _profileService.UpdateBannerAsync(authUserId.Value, stream, file.FileName);

        if (!result.IsSuccess)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<ProfileDto>(default, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
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
        var authUserId = GetAuthUserId();
        if (authUserId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<ProfileDto>(default, message: ResponseMessages.InvalidToken));

        var result = await _profileService.RemoveBannerAsync(authUserId.Value);

        if (!result.IsSuccess)
            return ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<ProfileDto>(default, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
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
        if (string.IsNullOrWhiteSpace(q))
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<PagedResult<SearchProfileDto>>(default, message: "Search query is required"));

        var currentProfileId = GetProfileId();
        var result = await _profileService.SearchProfilesAsync(
            q, currentProfileId, page, Math.Clamp(pageSize, 1, 50));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
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
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken));

        var result = await _profileService.BlockUserAsync(profileId.Value, username);

        if (!result.IsSuccess)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
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
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken));

        var result = await _profileService.UnblockUserAsync(profileId.Value, username);

        if (!result.IsSuccess)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }

    [Authorize]
    [HttpGet("api/profile/blocked")]
    [EndpointSummary("Usuarios bloqueados")]
    [EndpointDescription("Obtiene la lista paginada de usuarios bloqueados por el perfil autenticado.")]
    [ProducesResponseType<GenericResponse<PagedResult<BlockedUserDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<PagedResult<BlockedUserDto>>> GetBlockedUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<PagedResult<BlockedUserDto>>(default, message: ResponseMessages.InvalidToken));

        var result = await _profileService.GetBlockedUsersAsync(profileId.Value, page, Math.Clamp(pageSize, 1, 50));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }
}
