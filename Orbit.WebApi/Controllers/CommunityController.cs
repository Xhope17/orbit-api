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

[Route("api/communities")]
public class CommunityController : BaseController
{
    private readonly ICommunityService _communityService;

    public CommunityController(
        ICommunityService communityService)
    {
        _communityService = communityService;
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
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _communityService.CreateCommunityAsync(authUserId, request.Name, request.Description, request.IsPrivate);
        return ResponseStatus.Created(HttpContext, rsp);
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
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _communityService.UpdateCommunityAsync(authUserId, slug, request.Name, request.Description, request.IsPrivate);
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _communityService.DeleteCommunityAsync(authUserId, slug);
        return ResponseStatus.Ok(HttpContext, rsp);
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

        var rsp = await _communityService.GetCommunityAsync(slug, profileId);
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var rsp = await _communityService.SearchCommunitiesAsync(q, page, pageSize, profileId);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpGet("my")]
    [EndpointSummary("Mis comunidades")]
    [EndpointDescription("Obtiene las comunidades del usuario autenticado.")]
    [ProducesResponseType<GenericResponse<PagedResult<CommunitySummaryDto>>>(StatusCodes.Status200OK)]
    public async Task<GenericResponse<PagedResult<CommunitySummaryDto>>> GetMyCommunities([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);
        var rsp = await _communityService.GetMyCommunitiesAsync(profileId, page, pageSize);
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _communityService.JoinCommunityAsync(profileId, slug);
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _communityService.LeaveCommunityAsync(profileId, slug);
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _communityService.KickMemberAsync(authUserId, slug, targetProfileId);
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _communityService.AssignCoLeaderAsync(authUserId, slug, targetUsername);
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _communityService.RemoveCoLeaderAsync(authUserId, slug, targetUsername);
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var rsp = await _communityService.GetMembersAsync(slug, page, pageSize, profileId);
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _communityService.RequestJoinAsync(profileId, slug);
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);
        var rsp = await _communityService.GetJoinRequestsAsync(authUserId, slug, page, pageSize);
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _communityService.ApproveJoinRequestAsync(authUserId, requestId);
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _communityService.RejectJoinRequestAsync(authUserId, requestId);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpGet("join-requests/my")]
    [EndpointSummary("Mis solicitudes")]
    [EndpointDescription("Obtiene las solicitudes de unión del usuario autenticado.")]
    [ProducesResponseType<GenericResponse<PagedResult<CommunityJoinRequestResponse>>>(StatusCodes.Status200OK)]
    public async Task<GenericResponse<PagedResult<CommunityJoinRequestResponse>>> GetMyJoinRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);
        var rsp = await _communityService.GetMyJoinRequestsAsync(profileId, page, pageSize);
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _communityService.InviteMemberAsync(authUserId, slug, request.Username);
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);
        var rsp = await _communityService.GetCommunityInvitationsAsync(authUserId, slug, page, pageSize);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpGet("invitations/pending")]
    [EndpointSummary("Mis invitaciones")]
    [EndpointDescription("Obtiene las invitaciones pendientes del usuario autenticado.")]
    [ProducesResponseType<GenericResponse<PagedResult<CommunityInvitationResponse>>>(StatusCodes.Status200OK)]
    public async Task<GenericResponse<PagedResult<CommunityInvitationResponse>>> GetPendingInvitations([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);
        var rsp = await _communityService.GetPendingInvitationsAsync(profileId, page, pageSize);
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _communityService.AcceptInvitationAsync(profileId, invitationId);
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _communityService.DeclineInvitationAsync(profileId, invitationId);
        return ResponseStatus.Ok(HttpContext, rsp);
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
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);

        List<MediaUploadData>? mediaFiles = null;
        if (request.Media is not null && request.Media.Count > 0)
        {
            mediaFiles = request.Media
                .Where(f => f is not null)
                .Select(f => new MediaUploadData(f.OpenReadStream(), f.FileName))
                .ToList();
        }

        var rsp = await _communityService.CreateCommunityPostAsync(authUserId, slug, request.Content, mediaFiles);
        return ResponseStatus.Created(HttpContext, rsp);
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
        var rsp = await _communityService.GetCommunityPostsAsync(slug, profileId, page, pageSize);
        return ResponseStatus.Ok(HttpContext, rsp);
    }
}
