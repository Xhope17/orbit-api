using Orbit.Application.Common;
using Orbit.Application.Models.DTOs;

namespace Orbit.Application.Interfaces.Services;

public interface IProfileService
{
    Task<Result<ProfileDto>> GetProfileByUsernameAsync(string username, Guid? currentProfileId = null);
    Task<Result<ProfileDto>> UpdateProfileAsync(Guid authUserId, string? displayName, string? bio, bool? isPrivate);
    Task<Result<ProfileDto>> UpdateProfilePictureAsync(Guid authUserId, Stream fileStream, string fileName);
    Task<Result<ProfileDto>> RemoveProfilePictureAsync(Guid authUserId);
    Task<Result<ProfileDto>> UpdateBannerAsync(Guid authUserId, Stream fileStream, string fileName);
    Task<Result<ProfileDto>> RemoveBannerAsync(Guid authUserId);
    Task<Result<PagedResult<SearchProfileDto>>> SearchProfilesAsync(string query, Guid? currentProfileId, int page, int pageSize);

    Task<Result> BlockUserAsync(Guid blockerProfileId, string username);
    Task<Result> UnblockUserAsync(Guid blockerProfileId, string username);
    Task<Result<PagedResult<BlockedUserDto>>> GetBlockedUsersAsync(Guid profileId, int page, int pageSize);

    Task<Result> BanUserAsync(Guid moderatorProfileId, string username);
    Task<Result> UnbanUserAsync(Guid moderatorProfileId, string username);
}
