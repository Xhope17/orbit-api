using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbit.WebApi.Models;
using Orbit.WebApi.Validators;
using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Helpers;
using Orbit.Application.Models.Responses;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Services;
using Orbit.WebApi.Helpers;

namespace Orbit.WebApi.Controllers;

[Route("api/communities")]
public class CommunityController : BaseController
{
    private readonly ICommunityService _communityService;
    private readonly CreateCommunityValidator _createValidator;
    private readonly UpdateCommunityValidator _updateValidator;

    public CommunityController(
        ICommunityService communityService,
        CreateCommunityValidator createValidator,
        UpdateCommunityValidator updateValidator)
    {
        _communityService = communityService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [Authorize]
    [HttpPost]
    [EndpointSummary("Crear comunidad")]
    [EndpointDescription("Crea una nueva comunidad. El creador se convierte en owner automáticamente.")]
    [ProducesResponseType<GenericResponse<CommunityDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status409Conflict)]
    public async Task<GenericResponse<CommunityDto>> Create([FromBody] CreateCommunityRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<CommunityDto>(default, errors: validation.Errors.Select(e => e.ErrorMessage).ToList(), message: ResponseMessages.ValidationFailed));

        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<CommunityDto>(default, message: ResponseMessages.InvalidToken));

        var authUserId = GetAuthUserId();
        if (authUserId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<CommunityDto>(default, message: ResponseMessages.InvalidToken));

        var result = await _communityService.CreateCommunityAsync(authUserId.Value, request.Name, request.Description, request.IsPrivate);
        if (!result.IsSuccess)
            return ResponseStatus.Conflict(HttpContext, ResponseHelper.Create<CommunityDto>(default, message: result.Message));

        return ResponseStatus.Created(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }

    [Authorize]
    [HttpPut("{slug}")]
    [EndpointSummary("Actualizar comunidad")]
    [EndpointDescription("Actualiza los datos de la comunidad. Solo owner o co-leader.")]
    [ProducesResponseType<GenericResponse<CommunityDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<CommunityDto>> Update(string slug, [FromBody] UpdateCommunityRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<CommunityDto>(default, errors: validation.Errors.Select(e => e.ErrorMessage).ToList(), message: ResponseMessages.ValidationFailed));

        var authUserId = GetAuthUserId();
        if (authUserId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<CommunityDto>(default, message: ResponseMessages.InvalidToken));

        var result = await _communityService.UpdateCommunityAsync(authUserId.Value, slug, request.Name, request.Description, request.IsPrivate);
        if (!result.IsSuccess)
            return result.Message == ResponseMessages.CommunityNotFound
                ? ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<CommunityDto>(default, message: result.Message))
                : ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<CommunityDto>(default, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }

    [Authorize]
    [HttpDelete("{slug}")]
    [EndpointSummary("Eliminar comunidad")]
    [EndpointDescription("Elimina la comunidad. Solo owner.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<string>> Delete(string slug)
    {
        var authUserId = GetAuthUserId();
        if (authUserId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken));

        var result = await _communityService.DeleteCommunityAsync(authUserId.Value, slug);
        if (!result.IsSuccess)
            return result.Message == ResponseMessages.CommunityNotFound
                ? ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<string>(null, message: result.Message))
                : ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }

    [AllowAnonymous]
    [HttpGet("{slug}")]
    [EndpointSummary("Obtener comunidad")]
    [EndpointDescription("Obtiene los detalles de una comunidad por slug.")]
    [ProducesResponseType<GenericResponse<CommunityDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<CommunityDto>> GetBySlug(string slug)
    {
        var profileId = GetProfileId();

        var result = await _communityService.GetCommunityAsync(slug, profileId);
        if (!result.IsSuccess)
            return ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<CommunityDto>(default, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }

    [AllowAnonymous]
    [HttpGet]
    [EndpointSummary("Buscar comunidades")]
    [EndpointDescription("Lista comunidades públicas con búsqueda opcional.")]
    [ProducesResponseType<GenericResponse<PagedResult<CommunitySummaryDto>>>(StatusCodes.Status200OK)]
    public async Task<GenericResponse<PagedResult<CommunitySummaryDto>>> Search([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var profileId = GetProfileId();
        var result = await _communityService.SearchCommunitiesAsync(q, page, pageSize, profileId);
        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }

    [Authorize]
    [HttpGet("my")]
    [EndpointSummary("Mis comunidades")]
    [EndpointDescription("Obtiene las comunidades del usuario autenticado.")]
    [ProducesResponseType<GenericResponse<PagedResult<CommunitySummaryDto>>>(StatusCodes.Status200OK)]
    public async Task<GenericResponse<PagedResult<CommunitySummaryDto>>> GetMyCommunities([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<PagedResult<CommunitySummaryDto>>(default, message: ResponseMessages.InvalidToken));

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var result = await _communityService.GetMyCommunitiesAsync(profileId.Value, page, pageSize);
        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }

    [Authorize]
    [HttpPost("{slug}/join")]
    [EndpointSummary("Unirse a comunidad")]
    [EndpointDescription("El usuario autenticado se une a una comunidad pública.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<string>> Join(string slug)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken));

        var result = await _communityService.JoinCommunityAsync(profileId.Value, slug);
        if (!result.IsSuccess)
            return result.Message == ResponseMessages.CommunityNotFound
                ? ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<string>(null, message: result.Message))
                : ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }

    [Authorize]
    [HttpDelete("{slug}/leave")]
    [EndpointSummary("Salir de comunidad")]
    [EndpointDescription("El usuario autenticado abandona una comunidad.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<string>> Leave(string slug)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken));

        var result = await _communityService.LeaveCommunityAsync(profileId.Value, slug);
        if (!result.IsSuccess)
            return result.Message == ResponseMessages.CommunityNotFound
                ? ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<string>(null, message: result.Message))
                : ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }

    [Authorize]
    [HttpDelete("{slug}/members/{targetProfileId:guid}")]
    [EndpointSummary("Expulsar miembro")]
    [EndpointDescription("Expulsa a un miembro de la comunidad. Solo owner o co-leader.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<string>> KickMember(string slug, Guid targetProfileId)
    {
        var authUserId = GetAuthUserId();
        if (authUserId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken));

        var result = await _communityService.KickMemberAsync(authUserId.Value, slug, targetProfileId);
        if (!result.IsSuccess)
            return result.Message == ResponseMessages.CommunityNotFound || result.Message == ResponseMessages.NotMember
                ? ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<string>(null, message: result.Message))
                : ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }

    [Authorize]
    [HttpPost("{slug}/co-leaders/{targetUsername}")]
    [EndpointSummary("Asignar co-líder")]
    [EndpointDescription("Asigna a un miembro como co-líder. Solo owner.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<string>> AssignCoLeader(string slug, string targetUsername)
    {
        var authUserId = GetAuthUserId();
        if (authUserId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken));

        var result = await _communityService.AssignCoLeaderAsync(authUserId.Value, slug, targetUsername);
        if (!result.IsSuccess)
            return result.Message == ResponseMessages.CommunityNotFound || result.Message == ResponseMessages.ProfileNotFound
                ? ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<string>(null, message: result.Message))
                : result.Message == ResponseMessages.AlreadyCoLeader
                    ? ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message))
                    : ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }

    [Authorize]
    [HttpDelete("{slug}/co-leaders/{targetUsername}")]
    [EndpointSummary("Remover co-líder")]
    [EndpointDescription("Remueve a un co-líder y lo devuelve a member. Solo owner.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<string>> RemoveCoLeader(string slug, string targetUsername)
    {
        var authUserId = GetAuthUserId();
        if (authUserId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken));

        var result = await _communityService.RemoveCoLeaderAsync(authUserId.Value, slug, targetUsername);
        if (!result.IsSuccess)
            return result.Message == ResponseMessages.CommunityNotFound || result.Message == ResponseMessages.ProfileNotFound
                ? ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<string>(null, message: result.Message))
                : result.Message == ResponseMessages.NotCoLeader
                    ? ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message))
                    : ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }

    [AllowAnonymous]
    [HttpGet("{slug}/members")]
    [EndpointSummary("Miembros de comunidad")]
    [EndpointDescription("Obtiene los miembros de una comunidad. Privadas requieren ser miembro.")]
    [ProducesResponseType<GenericResponse<PagedResult<CommunityMemberResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<PagedResult<CommunityMemberResponse>>> GetMembers(string slug, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var profileId = GetProfileId();
        var result = await _communityService.GetMembersAsync(slug, page, pageSize, profileId);
        if (!result.IsSuccess)
            return result.Message == ResponseMessages.CommunityNotFound
                ? ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<PagedResult<CommunityMemberResponse>>(default, message: result.Message))
                : ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<PagedResult<CommunityMemberResponse>>(default, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }

    [Authorize]
    [HttpPost("{slug}/join-request")]
    [EndpointSummary("Solicitar unirse a comunidad")]
    [EndpointDescription("Envía una solicitud para unirse a una comunidad privada.")]
    [ProducesResponseType<GenericResponse<CommunityJoinRequestResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<CommunityJoinRequestResponse>> RequestJoin(string slug)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<CommunityJoinRequestResponse>(default, message: ResponseMessages.InvalidToken));

        var result = await _communityService.RequestJoinAsync(profileId.Value, slug);
        if (!result.IsSuccess)
            return result.Message == ResponseMessages.CommunityNotFound
                ? ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<CommunityJoinRequestResponse>(default, message: result.Message))
                : ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<CommunityJoinRequestResponse>(default, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }

    [Authorize]
    [HttpGet("{slug}/join-requests")]
    [EndpointSummary("Ver solicitudes pendientes")]
    [EndpointDescription("Obtiene las solicitudes de unión pendientes. Solo owner o co-leader.")]
    [ProducesResponseType<GenericResponse<PagedResult<CommunityJoinRequestResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<PagedResult<CommunityJoinRequestResponse>>> GetJoinRequests(string slug, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var authUserId = GetAuthUserId();
        if (authUserId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<PagedResult<CommunityJoinRequestResponse>>(default, message: ResponseMessages.InvalidToken));

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var result = await _communityService.GetJoinRequestsAsync(authUserId.Value, slug, page, pageSize);
        if (!result.IsSuccess)
            return result.Message == ResponseMessages.CommunityNotFound
                ? ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<PagedResult<CommunityJoinRequestResponse>>(default, message: result.Message))
                : ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<PagedResult<CommunityJoinRequestResponse>>(default, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }

    [Authorize]
    [HttpPost("join-requests/{requestId:guid}/approve")]
    [EndpointSummary("Aprobar solicitud")]
    [EndpointDescription("Aprueba una solicitud de unión. Solo owner o co-leader.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<string>> ApproveJoinRequest(Guid requestId)
    {
        var authUserId = GetAuthUserId();
        if (authUserId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken));

        var result = await _communityService.ApproveJoinRequestAsync(authUserId.Value, requestId);
        if (!result.IsSuccess)
            return result.Message == ResponseMessages.JoinRequestNotFound || result.Message == ResponseMessages.CommunityNotFound
                ? ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<string>(null, message: result.Message))
                : ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }

    [Authorize]
    [HttpPost("join-requests/{requestId:guid}/reject")]
    [EndpointSummary("Rechazar solicitud")]
    [EndpointDescription("Rechaza una solicitud de unión. Solo owner o co-leader.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<string>> RejectJoinRequest(Guid requestId)
    {
        var authUserId = GetAuthUserId();
        if (authUserId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken));

        var result = await _communityService.RejectJoinRequestAsync(authUserId.Value, requestId);
        if (!result.IsSuccess)
            return result.Message == ResponseMessages.JoinRequestNotFound || result.Message == ResponseMessages.CommunityNotFound
                ? ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<string>(null, message: result.Message))
                : ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }

    [Authorize]
    [HttpGet("join-requests/my")]
    [EndpointSummary("Mis solicitudes")]
    [EndpointDescription("Obtiene las solicitudes de unión del usuario autenticado.")]
    [ProducesResponseType<GenericResponse<PagedResult<CommunityJoinRequestResponse>>>(StatusCodes.Status200OK)]
    public async Task<GenericResponse<PagedResult<CommunityJoinRequestResponse>>> GetMyJoinRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<PagedResult<CommunityJoinRequestResponse>>(default, message: ResponseMessages.InvalidToken));

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var result = await _communityService.GetMyJoinRequestsAsync(profileId.Value, page, pageSize);
        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }

    [Authorize]
    [HttpPost("{slug}/invitations")]
    [EndpointSummary("Invitar miembro")]
    [EndpointDescription("Invita a un usuario a unirse a la comunidad. Solo owner o co-leader.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<string>> InviteMember(string slug, [FromBody] ModeratorRequest request)
    {
        var authUserId = GetAuthUserId();
        if (authUserId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken));

        var result = await _communityService.InviteMemberAsync(authUserId.Value, slug, request.Username);
        if (!result.IsSuccess)
            return result.Message == ResponseMessages.CommunityNotFound || result.Message == ResponseMessages.ProfileNotFound
                ? ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<string>(null, message: result.Message))
                : ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }

    [Authorize]
    [HttpGet("{slug}/invitations")]
    [EndpointSummary("Ver invitaciones pendientes")]
    [EndpointDescription("Obtiene las invitaciones pendientes de la comunidad. Solo owner o co-leader.")]
    [ProducesResponseType<GenericResponse<PagedResult<CommunityInvitationResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<PagedResult<CommunityInvitationResponse>>> GetCommunityInvitations(string slug, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var authUserId = GetAuthUserId();
        if (authUserId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<PagedResult<CommunityInvitationResponse>>(default, message: ResponseMessages.InvalidToken));

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var result = await _communityService.GetCommunityInvitationsAsync(authUserId.Value, slug, page, pageSize);
        if (!result.IsSuccess)
            return result.Message == ResponseMessages.CommunityNotFound
                ? ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<PagedResult<CommunityInvitationResponse>>(default, message: result.Message))
                : ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<PagedResult<CommunityInvitationResponse>>(default, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }

    [Authorize]
    [HttpGet("invitations/pending")]
    [EndpointSummary("Mis invitaciones")]
    [EndpointDescription("Obtiene las invitaciones pendientes del usuario autenticado.")]
    [ProducesResponseType<GenericResponse<PagedResult<CommunityInvitationResponse>>>(StatusCodes.Status200OK)]
    public async Task<GenericResponse<PagedResult<CommunityInvitationResponse>>> GetPendingInvitations([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<PagedResult<CommunityInvitationResponse>>(default, message: ResponseMessages.InvalidToken));

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var result = await _communityService.GetPendingInvitationsAsync(profileId.Value, page, pageSize);
        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }

    [Authorize]
    [HttpPost("invitations/{invitationId:guid}/accept")]
    [EndpointSummary("Aceptar invitación")]
    [EndpointDescription("Acepta una invitación para unirse a una comunidad.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<string>> AcceptInvitation(Guid invitationId)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken));

        var result = await _communityService.AcceptInvitationAsync(profileId.Value, invitationId);
        if (!result.IsSuccess)
            return result.Message == ResponseMessages.InvitationNotFound
                ? ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<string>(null, message: result.Message))
                : ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }

    [Authorize]
    [HttpPost("invitations/{invitationId:guid}/decline")]
    [EndpointSummary("Rechazar invitación")]
    [EndpointDescription("Rechaza una invitación para unirse a una comunidad.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<string>> DeclineInvitation(Guid invitationId)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken));

        var result = await _communityService.DeclineInvitationAsync(profileId.Value, invitationId);
        if (!result.IsSuccess)
            return result.Message == ResponseMessages.InvitationNotFound
                ? ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<string>(null, message: result.Message))
                : ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }

    [Authorize]
    [HttpPost("{slug}/posts")]
    [EndpointSummary("Crear post en comunidad")]
    [EndpointDescription("Crea una publicación en la comunidad. Solo miembros.")]
    [ProducesResponseType<GenericResponse<PostDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<PostDto>> CreatePost(string slug, [FromForm] CreatePostRequest request)
    {
        var authUserId = GetAuthUserId();
        if (authUserId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<PostDto>(default, message: ResponseMessages.InvalidToken));

        if (string.IsNullOrWhiteSpace(request.Content))
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<PostDto>(default, message: "Content is required"));

        List<MediaUploadData>? mediaFiles = null;
        if (request.Media is not null && request.Media.Count > 0)
        {
            mediaFiles = request.Media
                .Where(f => f is not null)
                .Select(f => new MediaUploadData(f.OpenReadStream(), f.FileName))
                .ToList();
        }

        var result = await _communityService.CreateCommunityPostAsync(authUserId.Value, slug, request.Content, mediaFiles);
        if (!result.IsSuccess)
            return result.Message switch
            {
                var msg when msg == ResponseMessages.CommunityNotFound || msg == ResponseMessages.ProfileNotFound => ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<PostDto>(default, message: result.Message)),
                _ => ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<PostDto>(default, message: result.Message))
            };

        return ResponseStatus.Created(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }

    [AllowAnonymous]
    [HttpGet("{slug}/posts")]
    [EndpointSummary("Posts de comunidad")]
    [EndpointDescription("Obtiene los posts de una comunidad. Privadas requieren ser miembro.")]
    [ProducesResponseType<GenericResponse<PagedResult<PostDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<PagedResult<PostDto>>> GetCommunityPosts(string slug, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var profileId = GetProfileId();
        var result = await _communityService.GetCommunityPostsAsync(slug, profileId, page, pageSize);
        if (!result.IsSuccess)
            return result.Message == ResponseMessages.CommunityNotFound
                ? ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<PagedResult<PostDto>>(default, message: result.Message))
                : ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<PagedResult<PostDto>>(default, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }
}
