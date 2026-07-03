using Orbit.Application.Common;

namespace Orbit.Application.Interfaces.Services;

public interface IRoleService
{
    Task<Result> AssignModeratorAsync(Guid adminProfileId, string targetUsername);
    Task<Result> RemoveModeratorAsync(Guid adminProfileId, string targetUsername);
}
