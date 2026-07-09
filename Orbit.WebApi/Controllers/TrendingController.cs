using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbit.Application.Models.Responses;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Services;
using Orbit.WebApi.Helpers;

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
    [ProducesResponseType<GenericResponse<List<TrendingHashtagDto>>>(StatusCodes.Status200OK)]
    public async Task<GenericResponse<List<TrendingHashtagDto>>> GetTrending([FromQuery] int hours = 24)
    {
        var rsp = await _hashtagService.GetTrendingHashtagsAsync(hours);
        return ResponseStatus.Ok(HttpContext, rsp);
    }
}
