using Orbit.Application.Common;
using Orbit.Application.Models.Responses;
using Orbit.Application.Models.DTOs;

namespace Orbit.Application.Interfaces.Services;

public interface IFollowService
{
    Task<GenericResponse<string>> FollowUserAsync(Guid followerProfileId, string username);
    Task<GenericResponse<string>> UnfollowUserAsync(Guid followerProfileId, string username);
    Task<GenericResponse<PagedResult<PostAuthorDto>>> GetFollowersAsync(string username, Guid? currentProfileId, int page, int pageSize);
    Task<GenericResponse<PagedResult<PostAuthorDto>>> GetFollowingAsync(string username, Guid? currentProfileId, int page, int pageSize);
}
