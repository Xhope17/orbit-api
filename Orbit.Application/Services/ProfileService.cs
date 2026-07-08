using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Enums;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.DataBase;
using Orbit.Domain.Entities;

namespace Orbit.Application.Services;

public class ProfileService : IProfileService
{
    private readonly IUnitOfWork _uow;
    private readonly ICloudinaryService _cloudinaryService;

    public ProfileService(
        IUnitOfWork uow,
        ICloudinaryService cloudinaryService)
    {
        _uow = uow;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<Result<ProfileResponse>> GetProfileByUsernameAsync(string username, Guid? currentProfileId = null)
    {
        var slug = username.ToLowerInvariant();
        var profile = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (profile is null)
            return Result<ProfileResponse>.Failure(ResponseMessages.ProfileNotFound);

        if (currentProfileId.HasValue && currentProfileId.Value != profile.Id)
        {
            var isBlocked = await _uow.userBanRepository.Get(b =>
                b.BlockerProfileId == profile.Id && b.BlockedProfileId == currentProfileId.Value);
            if (isBlocked is not null)
                return Result<ProfileResponse>.Failure(ResponseMessages.ProfileNotFound);
        }

        bool isFollowing = false;
        if (currentProfileId.HasValue && currentProfileId.Value != profile.Id)
        {
            var follow = await _uow.followRepository.Get(f =>
                f.FollowerId == currentProfileId.Value && f.FollowingId == profile.Id);
            isFollowing = follow is not null;
        }

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);
        return Result<ProfileResponse>.Success(BuildResponse(profile, prefixResponse, isFollowing));
    }

    public async Task<Result<ProfileResponse>> UpdateProfileAsync(Guid authUserId, string? displayName, string? bio, bool? isPrivate)
    {
        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result<ProfileResponse>.Failure(ResponseMessages.ProfileNotFound);

        if (displayName is not null) profile.DisplayName = displayName;
        if (bio is not null) profile.Bio = bio;
        if (isPrivate.HasValue) profile.IsPrivate = isPrivate.Value;

        profile.UpdatedAt = DateTime.UtcNow;
        await _uow.profileRepository.Update(profile);
        await _uow.SaveChangesAsync();

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);
        return Result<ProfileResponse>.Success(BuildResponse(profile, prefixResponse));
    }

    public async Task<Result<ProfileResponse>> UpdateProfilePictureAsync(Guid authUserId, Stream fileStream, string fileName)
    {
        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result<ProfileResponse>.Failure(ResponseMessages.ProfileNotFound);

        if (profile.ProfilePicturePublicId is not null)
        {
            await _cloudinaryService.DeleteAsync(profile.ProfilePicturePublicId);
        }

        var ext = Path.GetExtension(fileName);
        var uploadFileName = $"{authUserId}_{Guid.NewGuid()}{ext}";
        var uploadResult = await _cloudinaryService.UploadAsync(fileStream, uploadFileName, CloudinaryFolder.ProfilePics);

        if (!uploadResult.IsSuccess || uploadResult.Data is null)
            return Result<ProfileResponse>.Failure(ResponseMessages.FailedToUploadProfilePicture);

        profile.ProfilePictureUrl = uploadResult.Data.Url;
        profile.ProfilePicturePublicId = uploadResult.Data.PublicId;
        profile.UpdatedAt = DateTime.UtcNow;
        await _uow.profileRepository.Update(profile);
        await _uow.SaveChangesAsync();

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);
        return Result<ProfileResponse>.Success(BuildResponse(profile, prefixResponse));
    }

    public async Task<Result<ProfileResponse>> RemoveProfilePictureAsync(Guid authUserId)
    {
        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result<ProfileResponse>.Failure(ResponseMessages.ProfileNotFound);

        if (profile.ProfilePicturePublicId is not null)
        {
            await _cloudinaryService.DeleteAsync(profile.ProfilePicturePublicId);
        }

        profile.ProfilePictureUrl = null;
        profile.ProfilePicturePublicId = null;
        profile.UpdatedAt = DateTime.UtcNow;
        await _uow.profileRepository.Update(profile);
        await _uow.SaveChangesAsync();

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);
        return Result<ProfileResponse>.Success(BuildResponse(profile, prefixResponse));
    }

    public async Task<Result<ProfileResponse>> UpdateBannerAsync(Guid authUserId, Stream fileStream, string fileName)
    {
        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result<ProfileResponse>.Failure(ResponseMessages.ProfileNotFound);

        if (profile.BannerPublicId is not null)
        {
            await _cloudinaryService.DeleteAsync(profile.BannerPublicId);
        }

        var ext = Path.GetExtension(fileName);
        var uploadFileName = $"{authUserId}_{Guid.NewGuid()}{ext}";
        var uploadResult = await _cloudinaryService.UploadAsync(fileStream, uploadFileName, CloudinaryFolder.ProfileBanners);

        if (!uploadResult.IsSuccess || uploadResult.Data is null)
            return Result<ProfileResponse>.Failure(ResponseMessages.FailedToUploadBanner);

        profile.BannerUrl = uploadResult.Data.Url;
        profile.BannerPublicId = uploadResult.Data.PublicId;
        profile.UpdatedAt = DateTime.UtcNow;
        await _uow.profileRepository.Update(profile);
        await _uow.SaveChangesAsync();

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);
        return Result<ProfileResponse>.Success(BuildResponse(profile, prefixResponse));
    }

    public async Task<Result<ProfileResponse>> RemoveBannerAsync(Guid authUserId)
    {
        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result<ProfileResponse>.Failure(ResponseMessages.ProfileNotFound);

        if (profile.BannerPublicId is not null)
        {
            await _cloudinaryService.DeleteAsync(profile.BannerPublicId);
        }

        profile.BannerUrl = null;
        profile.BannerPublicId = null;
        profile.UpdatedAt = DateTime.UtcNow;
        await _uow.profileRepository.Update(profile);
        await _uow.SaveChangesAsync();

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);
        return Result<ProfileResponse>.Success(BuildResponse(profile, prefixResponse));
    }

    public async Task<Result<PagedResult<SearchProfileResponse>>> SearchProfilesAsync(
        string query, Guid? currentProfileId, int page, int pageSize)
    {
        var normalized = query.ToLowerInvariant();
        var skip = (page - 1) * pageSize;

        HashSet<Guid> excludedProfileIds = [];
        if (currentProfileId.HasValue)
        {
            var blockerProfileIds = (await _uow.userBanRepository.GetListAsync(b =>
                b.BlockedProfileId == currentProfileId.Value))
                .Select(b => b.BlockerProfileId);

            var blockedProfileIds = (await _uow.userBanRepository.GetListAsync(b =>
                b.BlockerProfileId == currentProfileId.Value))
                .Select(b => b.BlockedProfileId);

            excludedProfileIds = blockerProfileIds.Concat(blockedProfileIds).ToHashSet();
        }

        var profiles = await _uow.profileRepository.GetPagedAsync(
            p => (p.UsernameSlug.StartsWith(normalized) || p.DisplayName.ToLower().StartsWith(normalized))
                && !excludedProfileIds.Contains(p.Id),
            p => p.FollowersCount,
            skip,
            pageSize);

        var totalCount = await _uow.profileRepository.CountAsync(
            p => (p.UsernameSlug.StartsWith(normalized) || p.DisplayName.ToLower().StartsWith(normalized))
                && !excludedProfileIds.Contains(p.Id));

        HashSet<Guid> followedIds = [];
        if (currentProfileId.HasValue)
        {
            var profileIds = profiles.Select(p => p.Id).ToList();
            var existingFollows = await _uow.followRepository.GetListAsync(f =>
                f.FollowerId == currentProfileId.Value && profileIds.Contains(f.FollowingId));
            followedIds = existingFollows.Select(f => f.FollowingId).ToHashSet();
        }

        var items = profiles.Select(p => new SearchProfileResponse(
            p.Id,
            p.Username,
            p.DisplayName,
            p.ProfilePictureUrl,
            currentProfileId.HasValue && followedIds.Contains(p.Id)
        )).ToList();

        return Result<PagedResult<SearchProfileResponse>>.Success(new PagedResult<SearchProfileResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    public async Task<Result> BanUserAsync(Guid moderatorProfileId, string username)
    {
        var slug = username.ToLowerInvariant();
        var target = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (target is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        if (target.Id == moderatorProfileId)
            return Result.Failure(ResponseMessages.CannotBanYourself);

        if (target.IsBanned)
            return Result.Failure(ResponseMessages.UserAlreadyBanned);

        var adminRole = await _uow.roleRepository.Get(r => r.Name == "admin");
        if (adminRole is not null)
        {
            var isAdmin = await _uow.userRoleRepository.Get(ur =>
                ur.ProfileId == target.Id && ur.RoleId == adminRole.Id);
            if (isAdmin is not null)
                return Result.Failure(ResponseMessages.CannotBanAdmin);
        }

        target.IsBanned = true;
        target.BannedAt = DateTime.UtcNow;
        target.BannedByProfileId = moderatorProfileId;
        await _uow.profileRepository.Update(target);
        await _uow.SaveChangesAsync();

        return Result.Success(ResponseMessages.BanSuccessful);
    }

    public async Task<Result> UnbanUserAsync(Guid moderatorProfileId, string username)
    {
        var slug = username.ToLowerInvariant();
        var target = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (target is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        if (!target.IsBanned)
            return Result.Failure(ResponseMessages.UserNotBanned);

        target.IsBanned = false;
        target.BannedAt = null;
        target.BannedByProfileId = null;
        await _uow.profileRepository.Update(target);
        await _uow.SaveChangesAsync();

        return Result.Success(ResponseMessages.UnbanSuccessful);
    }

    public async Task<Result> BlockUserAsync(Guid blockerProfileId, string username)
    {
        var slug = username.ToLowerInvariant();
        var target = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (target is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        if (target.Id == blockerProfileId)
            return Result.Failure(ResponseMessages.CannotBlockYourself);

        var existingBan = await _uow.userBanRepository.Get(b =>
            b.BlockerProfileId == blockerProfileId && b.BlockedProfileId == target.Id);
        if (existingBan is not null)
            return Result.Failure(ResponseMessages.AlreadyBlocked);

        var blockedByTarget = await _uow.userBanRepository.Get(b =>
            b.BlockerProfileId == target.Id && b.BlockedProfileId == blockerProfileId);
        if (blockedByTarget is not null)
            return Result.Failure(ResponseMessages.BlockedByUser);

        var followToTarget = await _uow.followRepository.Get(f =>
            f.FollowerId == blockerProfileId && f.FollowingId == target.Id);
        if (followToTarget is not null)
        {
            await _uow.followRepository.Delete(followToTarget);
            target.FollowersCount = Math.Max(0, target.FollowersCount - 1);
            target.UpdatedAt = DateTime.UtcNow;
            await _uow.profileRepository.Update(target);
            await _uow.SaveChangesAsync();

            var blockerProfile = await _uow.profileRepository.Get(p => p.Id == blockerProfileId);
            if (blockerProfile is not null)
            {
                blockerProfile.FollowingCount = Math.Max(0, blockerProfile.FollowingCount - 1);
                blockerProfile.UpdatedAt = DateTime.UtcNow;
                await _uow.profileRepository.Update(blockerProfile);
                await _uow.SaveChangesAsync();
            }
        }

        var followFromTarget = await _uow.followRepository.Get(f =>
            f.FollowerId == target.Id && f.FollowingId == blockerProfileId);
        if (followFromTarget is not null)
        {
            await _uow.followRepository.Delete(followFromTarget);
            target.FollowingCount = Math.Max(0, target.FollowingCount - 1);
            target.UpdatedAt = DateTime.UtcNow;
            await _uow.profileRepository.Update(target);
            await _uow.SaveChangesAsync();

            var blockerProfile = await _uow.profileRepository.Get(p => p.Id == blockerProfileId);
            if (blockerProfile is not null)
            {
                blockerProfile.FollowersCount = Math.Max(0, blockerProfile.FollowersCount - 1);
                blockerProfile.UpdatedAt = DateTime.UtcNow;
                await _uow.profileRepository.Update(blockerProfile);
                await _uow.SaveChangesAsync();
            }
        }

        var ban = new UserBan
        {
            Id = Guid.NewGuid(),
            BlockerProfileId = blockerProfileId,
            BlockedProfileId = target.Id,
            CreatedAt = DateTime.UtcNow,
        };

        await _uow.userBanRepository.Create(ban);

        var blockerPostIds = await _uow.postRepository.GetListAsync(p => p.ProfileId == blockerProfileId);
        var blockerPostIdHashes = blockerPostIds.Select(p => p.Id).ToHashSet();

        if (blockerPostIdHashes.Count > 0)
        {
            var blockedComments = await _uow.commentRepository.GetListAsync(c =>
                c.ProfileId == target.Id && blockerPostIdHashes.Contains(c.PostId));

            foreach (var comment in blockedComments)
            {
                comment.IsActive = false;
                await _uow.commentRepository.Update(comment);

                var post = blockerPostIds.FirstOrDefault(p => p.Id == comment.PostId);
                if (post is not null)
                {
                    post.CommentCount = Math.Max(0, post.CommentCount - 1);
                    await _uow.postRepository.Update(post);
                }

                if (comment.ParentCommentId.HasValue)
                {
                    var parentComment = await _uow.commentRepository.Get(c => c.Id == comment.ParentCommentId.Value);
                    if (parentComment is not null)
                    {
                        parentComment.ReplyCount = Math.Max(0, parentComment.ReplyCount - 1);
                        await _uow.commentRepository.Update(parentComment);
                    }
                }
            }
        }

        await _uow.SaveChangesAsync();
        return Result.Success(ResponseMessages.BlockSuccessful);
    }

    public async Task<Result> UnblockUserAsync(Guid blockerProfileId, string username)
    {
        var slug = username.ToLowerInvariant();
        var target = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (target is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        var ban = await _uow.userBanRepository.Get(b =>
            b.BlockerProfileId == blockerProfileId && b.BlockedProfileId == target.Id);
        if (ban is null)
            return Result.Failure(ResponseMessages.NotBlocked);

        await _uow.userBanRepository.Delete(ban);
        await _uow.SaveChangesAsync();
        return Result.Success(ResponseMessages.UnblockSuccessful);
    }

    public async Task<Result<PagedResult<BlockedUserResponse>>> GetBlockedUsersAsync(
        Guid profileId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        var bans = await _uow.userBanRepository.GetPagedAsync(
            b => b.BlockerProfileId == profileId,
            b => b.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _uow.userBanRepository.CountAsync(b => b.BlockerProfileId == profileId);

        var blockedProfileIds = bans.Select(b => b.BlockedProfileId).Distinct().ToList();
        var profiles = blockedProfileIds.Count > 0
            ? await _uow.profileRepository.GetListAsync(p => blockedProfileIds.Contains(p.Id))
            : [];
        var profileMap = profiles.ToDictionary(p => p.Id);

        var banMap = bans.ToDictionary(b => b.BlockedProfileId);

        var items = blockedProfileIds
            .Select(pid =>
            {
                var p = profileMap.GetValueOrDefault(pid);
                var ban = banMap.GetValueOrDefault(pid);
                return p is not null && ban is not null
                    ? new BlockedUserResponse(p.Id, p.Username, p.DisplayName, p.ProfilePictureUrl, ban.CreatedAt)
                    : null;
            })
            .OfType<BlockedUserResponse>()
            .ToList();

        return Result<PagedResult<BlockedUserResponse>>.Success(new PagedResult<BlockedUserResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    private async Task<UserPrefixResponse?> GetPrefixAsync(Guid? prefixId)
    {
        if (!prefixId.HasValue) return null;

        var prefix = await _uow.userPrefixRepository.Get(p => p.Id == prefixId.Value);
        return prefix is null ? null : new UserPrefixResponse(prefix.Id, prefix.Name, prefix.Color, prefix.IconUrl);
    }

    private static ProfileResponse BuildResponse(Orbit.Domain.Entities.Profile profile, UserPrefixResponse? prefix, bool isFollowing = false)
    {
        return new ProfileResponse(
            profile.Id,
            profile.Username,
            profile.DisplayName,
            profile.ProfilePictureUrl,
            profile.BannerUrl,
            profile.Bio,
            profile.FollowersCount,
            profile.FollowingCount,
            profile.IsVerified,
            prefix,
            isFollowing
        );
    }
}
