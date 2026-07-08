using Orbit.Application.Models.DTOs;

namespace Orbit.Application.Interfaces.Services;

public interface IHashtagService
{
    Task ProcessPostHashtags(Guid postId, string? content);
    Task<List<TrendingHashtagDto>> GetTrendingHashtagsAsync(int hours = 24);
}
