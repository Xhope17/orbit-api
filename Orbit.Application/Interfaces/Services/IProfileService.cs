using Orbit.Application.Common;
using Orbit.Application.Models.Responses;
using Orbit.Application.Models.DTOs;

namespace Orbit.Application.Interfaces.Services;

public interface IProfileService
{
    Task<GenericResponse<ProfileDto>> GetProfileByUsernameAsync(string username, Guid? currentProfileId = null);
    Task<GenericResponse<ProfileDto>> UpdateProfileAsync(Guid authUserId, string? displayName, string? bio, bool? isPrivate);
    Task<GenericResponse<ProfileDto>> UpdateProfilePictureAsync(Guid authUserId, Stream fileStream, string fileName);
    Task<GenericResponse<ProfileDto>> RemoveProfilePictureAsync(Guid authUserId);
    Task<GenericResponse<ProfileDto>> UpdateBannerAsync(Guid authUserId, Stream fileStream, string fileName);
    Task<GenericResponse<ProfileDto>> RemoveBannerAsync(Guid authUserId);
    Task<GenericResponse<PagedResult<SearchProfileDto>>> SearchProfilesAsync(string query, Guid? currentProfileId, int page, int pageSize);

    Task<GenericResponse<string>> BlockUserAsync(Guid blockerProfileId, string username);
    Task<GenericResponse<string>> UnblockUserAsync(Guid blockerProfileId, string username);
    Task<GenericResponse<PagedResult<BlockedUserDto>>> GetBlockedUsersAsync(Guid profileId, int page, int pageSize);

    Task<GenericResponse<string>> BanUserAsync(Guid moderatorProfileId, string username);
    Task<GenericResponse<string>> UnbanUserAsync(Guid moderatorProfileId, string username);
}
