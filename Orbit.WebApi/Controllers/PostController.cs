using System.Security.Claims;
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

[ApiController]
public class PostController : ControllerBase
{
    private readonly IPostService _postService;

    public PostController(IPostService postService)
    {
        _postService = postService;
    }

    [Authorize]
    [HttpPost("api/posts")]
    [EndpointSummary("Crear publicación")]
    [EndpointDescription("Crea una nueva publicación con contenido opcional y archivos multimedia.")]
    [ProducesResponseType<GenericResponse<PostDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<PostDto>> Create([FromForm] CreatePostRequest request)
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

        var rsp = await _postService.CreatePostAsync(authUserId, request.Content, mediaFiles);
        return ResponseStatus.Created(HttpContext, rsp);
    }

    [AllowAnonymous]
    [HttpGet("api/posts/{id:guid}")]
    [EndpointSummary("Obtener publicación")]
    [EndpointDescription("Obtiene una publicación por su ID.")]
    [ProducesResponseType<GenericResponse<PostDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<PostDto>> GetById(Guid id)
    {
        var currentProfileId = GetProfileId();
        var rsp = await _postService.GetPostAsync(id, currentProfileId);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [AllowAnonymous]
    [HttpGet("api/posts/general")]
    [EndpointSummary("Feed general")]
    [EndpointDescription("Obtiene todas las publicaciones generales (no de comunidades) de forma paginada.")]
    [ProducesResponseType<GenericResponse<PagedResult<PostDto>>>(StatusCodes.Status200OK)]
    public async Task<GenericResponse<PagedResult<PostDto>>> GetGeneralPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var currentProfileId = GetProfileId();
        var rsp = await _postService.GetGeneralPostsAsync(currentProfileId, page, Math.Clamp(pageSize, 1, 100));
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpGet("api/posts/following")]
    [EndpointSummary("Feed de seguidos")]
    [EndpointDescription("Obtiene las publicaciones de los usuarios a los que sigues.")]
    [ProducesResponseType<GenericResponse<PagedResult<PostDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<PagedResult<PostDto>>> GetFollowingPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _postService.GetFollowingPostsAsync(profileId, page, Math.Clamp(pageSize, 1, 100));
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [AllowAnonymous]
    [HttpGet("api/profiles/{username}/posts")]
    [EndpointSummary("Publicaciones de perfil")]
    [EndpointDescription("Obtiene las publicaciones de un perfil específico por username.")]
    [ProducesResponseType<GenericResponse<PagedResult<PostDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<PagedResult<PostDto>>> GetProfilePosts(string username, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var currentProfileId = GetProfileId();
        var rsp = await _postService.GetProfilePostsAsync(username, currentProfileId, page, Math.Clamp(pageSize, 1, 100));
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpPut("api/posts/{id:guid}")]
    [EndpointSummary("Actualizar publicación")]
    [EndpointDescription("Actualiza el contenido y/o multimedia de una publicación existente.")]
    [ProducesResponseType<GenericResponse<PostDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<PostDto>> Update(Guid id, [FromForm] UpdatePostRequest request)
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

        var rsp = await _postService.UpdatePostAsync(authUserId, id, request.Content, mediaFiles);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpDelete("api/posts/{id:guid}")]
    [EndpointSummary("Eliminar publicación")]
    [EndpointDescription("Elimina una publicación existente del usuario autenticado.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<string>> Delete(Guid id)
    {
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _postService.DeletePostAsync(authUserId, id);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpPost("api/posts/{id:guid}/repost")]
    [EndpointSummary("Repostear publicación")]
    [EndpointDescription("Comparte una publicación original sin mensaje adicional. Aparece en tu perfil como repost.")]
    [ProducesResponseType<GenericResponse<PostDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<PostDto>> Repost(Guid id)
    {
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _postService.RepostPostAsync(authUserId, id);
        return ResponseStatus.Created(HttpContext, rsp);
    }

    [Authorize]
    [HttpPost("api/posts/{id:guid}/thread")]
    [EndpointSummary("Crear hilo")]
    [EndpointDescription("Crea un hilo a partir de una publicación existente, con un mensaje adicional.")]
    [ProducesResponseType<GenericResponse<PostDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<PostDto>> Thread(Guid id, [FromBody] CreateThreadRequest request)
    {
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _postService.ThreadPostAsync(authUserId, id, request.Content);
        return ResponseStatus.Created(HttpContext, rsp);
    }

    [Authorize]
    [HttpPost("api/posts/{id:guid}/like")]
    [EndpointSummary("Dar like")]
    [EndpointDescription("Da like a una publicación.")]
    [ProducesResponseType<GenericResponse<PostLikeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<PostLikeResponse>> Like(Guid id)
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _postService.LikePostAsync(profileId, id);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpDelete("api/posts/{id:guid}/like")]
    [EndpointSummary("Quitar like")]
    [EndpointDescription("Quita el like de una publicación.")]
    [ProducesResponseType<GenericResponse<PostLikeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<PostLikeResponse>> Unlike(Guid id)
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _postService.UnlikePostAsync(profileId, id);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpPost("api/posts/{id:guid}/comments")]
    [EndpointSummary("Crear comentario")]
    [EndpointDescription("Crea un comentario en una publicación.")]
    [ProducesResponseType<GenericResponse<CommentDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<CommentDto>> CreateComment(Guid id, [FromBody] CreateCommentRequest request)
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _postService.CreateCommentAsync(profileId, id, request.Content, request.ParentCommentId);
        return ResponseStatus.Created(HttpContext, rsp);
    }

    [AllowAnonymous]
    [HttpGet("api/posts/{id:guid}/comments")]
    [EndpointSummary("Obtener comentarios")]
    [EndpointDescription("Obtiene los comentarios de una publicación.")]
    [ProducesResponseType<GenericResponse<PagedResult<CommentDto>>>(StatusCodes.Status200OK)]
    public async Task<GenericResponse<PagedResult<CommentDto>>> GetComments(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var currentProfileId = GetProfileId();
        var rsp = await _postService.GetCommentsAsync(id, currentProfileId, page, Math.Clamp(pageSize, 1, 100));
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [AllowAnonymous]
    [HttpGet("api/posts/search")]
    [EndpointSummary("Buscar publicaciones")]
    [EndpointDescription("Busca publicaciones cuyo contenido contenga el texto especificado.")]
    [ProducesResponseType<GenericResponse<PagedResult<PostDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    public async Task<GenericResponse<PagedResult<PostDto>>> SearchPosts(
        [FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var currentProfileId = GetProfileId();
        var rsp = await _postService.SearchPostsAsync(q, currentProfileId, page, Math.Clamp(pageSize, 1, 100));
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [AllowAnonymous]
    [HttpGet("api/comments/{id:guid}/replies")]
    [EndpointSummary("Obtener respuestas de comentario")]
    [EndpointDescription("Obtiene las respuestas paginadas de un comentario específico.")]
    [ProducesResponseType<GenericResponse<PagedResult<CommentDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<PagedResult<CommentDto>>> GetCommentReplies(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var currentProfileId = GetProfileId();
        var rsp = await _postService.GetCommentRepliesAsync(id, currentProfileId, page, Math.Clamp(pageSize, 1, 100));
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpPost("api/comments/{id:guid}/like")]
    [EndpointSummary("Dar like a comentario")]
    [EndpointDescription("Da like a un comentario.")]
    [ProducesResponseType<GenericResponse<CommentLikeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<CommentLikeResponse>> LikeComment(Guid id)
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _postService.LikeCommentAsync(profileId, id);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpDelete("api/comments/{id:guid}/like")]
    [EndpointSummary("Quitar like a comentario")]
    [EndpointDescription("Quita el like de un comentario.")]
    [ProducesResponseType<GenericResponse<CommentLikeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<CommentLikeResponse>> UnlikeComment(Guid id)
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _postService.UnlikeCommentAsync(profileId, id);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpDelete("api/comments/{id:guid}")]
    [EndpointSummary("Eliminar comentario")]
    [EndpointDescription("Elimina un comentario existente.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<string>> DeleteComment(Guid id)
    {
        var authUserId = GetAuthUserId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _postService.DeleteCommentAsync(authUserId, id);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    private Guid? GetAuthUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst(ClaimConstants.Sub)?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var authUserId))
            return null;
        return authUserId;
    }

    private Guid? GetProfileId()
    {
        var profileIdClaim = User.FindFirst(ClaimConstants.ProfileId)?.Value;
        if (profileIdClaim is null || !Guid.TryParse(profileIdClaim, out var profileId))
            return null;
        return profileId;
    }
}
