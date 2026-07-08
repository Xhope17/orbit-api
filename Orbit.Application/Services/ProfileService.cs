using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Enums;
using Orbit.Application.Helpers;
using Orbit.Application.Models.Responses;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.DataBase;
using Orbit.Domain.Entities;
using Orbit.Domain.Exceptions;

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

    public async Task<GenericResponse<ProfileDto>> GetProfileByUsernameAsync(string username, Guid? currentProfileId = null)
    {
        var slug = username.ToLowerInvariant();
        var profile = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (profile is null)
            throw new NotFoundException(ResponseMessages.ProfileNotFound);

        if (currentProfileId.HasValue && currentProfileId.Value != profile.Id)
        {
            var isBlocked = await _uow.userBanRepository.Get(b =>
                b.BlockerProfileId == profile.Id && b.BlockedProfileId == currentProfileId.Value);
            if (isBlocked is not null)
                throw new NotFoundException(ResponseMessages.ProfileNotFound);
        }

        bool isFollowing = false;
        if (currentProfileId.HasValue && currentProfileId.Value != profile.Id)
        {
            var follow = await _uow.followRepository.Get(f =>
                f.FollowerId == currentProfileId.Value && f.FollowingId == profile.Id);
            isFollowing = follow is not null;
        }

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);
        return ResponseHelper.Create(data: BuildResponse(profile, prefixResponse, isFollowing));
    }

    public async Task<GenericResponse<ProfileDto>> UpdateProfileAsync(Guid authUserId, string? displayName, string? bio, bool? isPrivate)
    {
        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            throw new NotFoundException(ResponseMessages.ProfileNotFound);

        if (displayName is not null) profile.DisplayName = displayName;
        if (bio is not null) profile.Bio = bio;
        if (isPrivate.HasValue) profile.IsPrivate = isPrivate.Value;

        profile.UpdatedAt = DateTime.UtcNow;
        await _uow.profileRepository.Update(profile);
        await _uow.SaveChangesAsync();

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);
        return ResponseHelper.Create(data: BuildResponse(profile, prefixResponse));
    }

    public async Task<GenericResponse<ProfileDto>> UpdateProfilePictureAsync(Guid authUserId, Stream fileStream, string fileName)
    {
        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            throw new NotFoundException(ResponseMessages.ProfileNotFound);

        if (profile.ProfilePicturePublicId is not null)
        {
            await _cloudinaryService.DeleteAsync(profile.ProfilePicturePublicId);
        }

        var ext = Path.GetExtension(fileName);
        var uploadFileName = $"{authUserId}_{Guid.NewGuid()}{ext}";
        var uploadResult = await _cloudinaryService.UploadAsync(fileStream, uploadFileName, CloudinaryFolder.ProfilePics);

        if (!uploadResult.IsSuccess || uploadResult.Data is null)
            throw new BadRequestException(ResponseMessages.FailedToUploadProfilePicture);

        profile.ProfilePictureUrl = uploadResult.Data.Url;
        profile.ProfilePicturePublicId = uploadResult.Data.PublicId;
        profile.UpdatedAt = DateTime.UtcNow;
        await _uow.profileRepository.Update(profile);
        await _uow.SaveChangesAsync();

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);
        return ResponseHelper.Create(data: BuildResponse(profile, prefixResponse));
    }

    public async Task<GenericResponse<ProfileDto>> RemoveProfilePictureAsync(Guid authUserId)
    {
        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            throw new NotFoundException(ResponseMessages.ProfileNotFound);

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
        return ResponseHelper.Create(data: BuildResponse(profile, prefixResponse));
    }

    public async Task<GenericResponse<ProfileDto>> UpdateBannerAsync(Guid authUserId, Stream fileStream, string fileName)
    {
        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            throw new NotFoundException(ResponseMessages.ProfileNotFound);

        if (profile.BannerPublicId is not null)
        {
            await _cloudinaryService.DeleteAsync(profile.BannerPublicId);
        }

        var ext = Path.GetExtension(fileName);
        var uploadFileName = $"{authUserId}_{Guid.NewGuid()}{ext}";
        var uploadResult = await _cloudinaryService.UploadAsync(fileStream, uploadFileName, CloudinaryFolder.ProfileBanners);

        if (!uploadResult.IsSuccess || uploadResult.Data is null)
            throw new BadRequestException(ResponseMessages.FailedToUploadBanner);

        profile.BannerUrl = uploadResult.Data.Url;
        profile.BannerPublicId = uploadResult.Data.PublicId;
        profile.UpdatedAt = DateTime.UtcNow;
        await _uow.profileRepository.Update(profile);
        await _uow.SaveChangesAsync();

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);
        return ResponseHelper.Create(data: BuildResponse(profile, prefixResponse));
    }

    public async Task<GenericResponse<ProfileDto>> RemoveBannerAsync(Guid authUserId)
    {
        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            throw new NotFoundException(ResponseMessages.ProfileNotFound);

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
        return ResponseHelper.Create(data: BuildResponse(profile, prefixResponse));
    }

    public async Task<GenericResponse<PagedResult<SearchProfileDto>>> SearchProfilesAsync(
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

        var items = profiles.Select(p => new SearchProfileDto(
            p.Id,
            p.Username,
            p.DisplayName,
            p.ProfilePictureUrl,
            currentProfileId.HasValue && followedIds.Contains(p.Id)
        )).ToList();

        return ResponseHelper.Create(data: new PagedResult<SearchProfileDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    public async Task<GenericResponse<string>> BanUserAsync(Guid moderatorProfileId, string username)
    {
        var slug = username.ToLowerInvariant();
        var target = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (target is null)
            throw new NotFoundException(ResponseMessages.ProfileNotFound);

        if (target.Id == moderatorProfileId)
            throw new BadRequestException(ResponseMessages.CannotBanYourself);

        if (target.IsBanned)
            throw new BadRequestException(ResponseMessages.UserAlreadyBanned);

        var adminRole = await _uow.roleRepository.Get(r => r.Name == "admin");
        if (adminRole is not null)
        {
            var isAdmin = await _uow.userRoleRepository.Get(ur =>
                ur.ProfileId == target.Id && ur.RoleId == adminRole.Id);
            if (isAdmin is not null)
                throw new BadRequestException(ResponseMessages.CannotBanAdmin);
        }

        target.IsBanned = true;
        target.BannedAt = DateTime.UtcNow;
        target.BannedByProfileId = moderatorProfileId;
        await _uow.profileRepository.Update(target);
        await _uow.SaveChangesAsync();

        return ResponseHelper.Create<string>(default, message: ResponseMessages.BanSuccessful);
    }

    public async Task<GenericResponse<string>> UnbanUserAsync(Guid moderatorProfileId, string username)
    {
        var slug = username.ToLowerInvariant();
        var target = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (target is null)
            throw new NotFoundException(ResponseMessages.ProfileNotFound);

        if (!target.IsBanned)
            throw new BadRequestException(ResponseMessages.UserNotBanned);

        target.IsBanned = false;
        target.BannedAt = null;
        target.BannedByProfileId = null;
        await _uow.profileRepository.Update(target);
        await _uow.SaveChangesAsync();

        return ResponseHelper.Create<string>(default, message: ResponseMessages.UnbanSuccessful);
    }

    public async Task<GenericResponse<string>> BlockUserAsync(Guid blockerProfileId, string username)
    {
        var slug = username.ToLowerInvariant();
        var target = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (target is null)
            throw new NotFoundException(ResponseMessages.ProfileNotFound);

        if (target.Id == blockerProfileId)
            throw new BadRequestException(ResponseMessages.CannotBlockYourself);

        var existingBan = await _uow.userBanRepository.Get(b =>
            b.BlockerProfileId == blockerProfileId && b.BlockedProfileId == target.Id);
        if (existingBan is not null)
            throw new BadRequestException(ResponseMessages.AlreadyBlocked);

        var blockedByTarget = await _uow.userBanRepository.Get(b =>
            b.BlockerProfileId == target.Id && b.BlockedProfileId == blockerProfileId);
        if (blockedByTarget is not null)
            throw new BadRequestException(ResponseMessages.BlockedByUser);

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
        return ResponseHelper.Create<string>(default, message: ResponseMessages.BlockSuccessful);
    }

    public async Task<GenericResponse<string>> UnblockUserAsync(Guid blockerProfileId, string username)
    {
        var slug = username.ToLowerInvariant();
        var target = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (target is null)
            throw new NotFoundException(ResponseMessages.ProfileNotFound);

        var ban = await _uow.userBanRepository.Get(b =>
            b.BlockerProfileId == blockerProfileId && b.BlockedProfileId == target.Id);
        if (ban is null)
            throw new BadRequestException(ResponseMessages.NotBlocked);

        await _uow.userBanRepository.Delete(ban);
        await _uow.SaveChangesAsync();
        return ResponseHelper.Create<string>(default, message: ResponseMessages.UnblockSuccessful);
    }

    public async Task<GenericResponse<PagedResult<BlockedUserDto>>> GetBlockedUsersAsync(
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
                    ? new BlockedUserDto(p.Id, p.Username, p.DisplayName, p.ProfilePictureUrl, ban.CreatedAt)
                    : null;
            })
            .OfType<BlockedUserDto>()
            .ToList();

        return ResponseHelper.Create(data: new PagedResult<BlockedUserDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    private async Task<UserPrefixDto?> GetPrefixAsync(Guid? prefixId)
    {
        if (!prefixId.HasValue) return null;

        var prefix = await _uow.userPrefixRepository.Get(p => p.Id == prefixId.Value);
        return prefix is null ? null : new UserPrefixDto(prefix.Id, prefix.Name, prefix.Color, prefix.IconUrl);
    }

    private static ProfileDto BuildResponse(Orbit.Domain.Entities.Profile profile, UserPrefixDto? prefix, bool isFollowing = false)
    {
        return new ProfileDto(
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
