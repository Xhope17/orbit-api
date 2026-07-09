using Orbit.Application.Models.DTOs;
using Orbit.Application.Models.Responses;

namespace Orbit.Application.Interfaces.Services;

public interface IHashtagService
{
    Task ProcessPostHashtags(Guid postId, string? content);
    Task<GenericResponse<List<TrendingHashtagDto>>> GetTrendingHashtagsAsync(int hours = 24);
}
