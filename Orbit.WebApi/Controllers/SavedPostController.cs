using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Services;

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
    [ProducesResponseType<Result<SaveResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SavePost(Guid postId)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return Unauthorized(new { isSuccess = false, message = ResponseMessages.InvalidToken });

        var result = await _postService.SavePostAsync(profileId.Value, postId);
        if (!result.IsSuccess)
            return NotFound(new { isSuccess = false, message = result.Message });

        return Ok(new { isSuccess = true, message = result.Message, data = result.Data });
    }

    [Authorize]
    [HttpDelete("{postId:guid}")]
    [EndpointSummary("Desguardar post")]
    [EndpointDescription("Elimina un post de la lista de guardados del usuario autenticado.")]
    [ProducesResponseType<Result<SaveResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnsavePost(Guid postId)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return Unauthorized(new { isSuccess = false, message = ResponseMessages.InvalidToken });

        var result = await _postService.UnsavePostAsync(profileId.Value, postId);
        if (!result.IsSuccess)
            return NotFound(new { isSuccess = false, message = result.Message });

        return Ok(new { isSuccess = true, message = result.Message, data = result.Data });
    }

    [Authorize]
    [HttpGet]
    [EndpointSummary("Posts guardados")]
    [EndpointDescription("Obtiene los posts guardados por el usuario autenticado, paginados.")]
    [ProducesResponseType<Result<PagedResult<PostResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSavedPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return Unauthorized(new { isSuccess = false, message = ResponseMessages.InvalidToken });

        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var result = await _postService.GetSavedPostsAsync(profileId.Value, page, pageSize);
        return Ok(new { isSuccess = true, data = result.Data });
    }
}
