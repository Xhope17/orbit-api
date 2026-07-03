using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Services;

namespace Orbit.WebApi.Controllers;

[AllowAnonymous]
[ApiController]
public class TrendingController : ControllerBase
{
    private readonly IHashtagService _hashtagService;

    public TrendingController(IHashtagService hashtagService)
    {
        _hashtagService = hashtagService;
    }

    [HttpGet("api/trending")]
    [EndpointSummary("Obtener tendencias")]
    [EndpointDescription("Devuelve los hashtags más usados en las últimas 24 horas.")]
    [ProducesResponseType<List<TrendingHashtagResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrending([FromQuery] int hours = 24)
    {
        var trending = await _hashtagService.GetTrendingHashtagsAsync(hours);
        return Ok(new { isSuccess = true, data = trending });
    }
}
