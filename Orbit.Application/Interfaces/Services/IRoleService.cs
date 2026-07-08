using Orbit.Application.Models.Responses;

namespace Orbit.Application.Interfaces.Services;

public interface IRoleService
{
    Task<GenericResponse<string>> AssignModeratorAsync(Guid adminProfileId, string targetUsername);
    Task<GenericResponse<string>> RemoveModeratorAsync(Guid adminProfileId, string targetUsername);
}
