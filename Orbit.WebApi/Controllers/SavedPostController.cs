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

[Route("api/saved-posts")]
public class SavedPostController : BaseController
{
    private readonly IPostService _postService;

    public SavedPostController(IPostService postService)
    {
        _postService = postService;
    }

    [Authorize]
    [HttpPost("{postId:guid}")]
    [EndpointSummary("Guardar post")]
    [EndpointDescription("Guarda un post en la lista del usuario autenticado.")]
    [ProducesResponseType<GenericResponse<PostSaveResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<PostSaveResponse>> SavePost(Guid postId)
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _postService.SavePostAsync(profileId, postId);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpDelete("{postId:guid}")]
    [EndpointSummary("Desguardar post")]
    [EndpointDescription("Elimina un post de la lista de guardados del usuario autenticado.")]
    [ProducesResponseType<GenericResponse<PostSaveResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<PostSaveResponse>> UnsavePost(Guid postId)
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _postService.UnsavePostAsync(profileId, postId);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [Authorize]
    [HttpGet]
    [EndpointSummary("Posts guardados")]
    [EndpointDescription("Obtiene los posts guardados por el usuario autenticado, paginados.")]
    [ProducesResponseType<GenericResponse<PagedResult<PostDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<PagedResult<PostDto>>> GetSavedPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);
        var rsp = await _postService.GetSavedPostsAsync(profileId, page, pageSize);
        return ResponseStatus.Ok(HttpContext, rsp);
    }
}
