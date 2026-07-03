using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Enums;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;

namespace Orbit.Application.Services;

public class ProfileService : IProfileService
{
    private readonly IGenericRepository<Orbit.Domain.Entities.Profile> _profileRepo;
    private readonly IGenericRepository<UserPrefix> _prefixRepo;
    private readonly IGenericRepository<Follow> _followRepo;
    private readonly IGenericRepository<UserBan> _userBanRepo;
    private readonly IGenericRepository<Role> _roleRepo;
    private readonly IGenericRepository<UserRole> _userRoleRepo;
    private readonly IGenericRepository<Orbit.Domain.Entities.Post> _postRepo;
    private readonly IGenericRepository<Comment> _commentRepo;
    private readonly ICloudinaryService _cloudinaryService;

    public ProfileService(
        IGenericRepository<Orbit.Domain.Entities.Profile> profileRepo,
        IGenericRepository<UserPrefix> prefixRepo,
        IGenericRepository<Follow> followRepo,
        IGenericRepository<UserBan> userBanRepo,
        IGenericRepository<Role> roleRepo,
        IGenericRepository<UserRole> userRoleRepo,
        IGenericRepository<Orbit.Domain.Entities.Post> postRepo,
        IGenericRepository<Comment> commentRepo,
        ICloudinaryService cloudinaryService)
    {
        _profileRepo = profileRepo;
        _prefixRepo = prefixRepo;
        _followRepo = followRepo;
        _userBanRepo = userBanRepo;
        _roleRepo = roleRepo;
        _userRoleRepo = userRoleRepo;
        _postRepo = postRepo;
        _commentRepo = commentRepo;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<Result<ProfileResponse>> GetProfileByUsernameAsync(string username, Guid? currentProfileId = null)
    {
        var slug = username.ToLowerInvariant();
        var profile = await _profileRepo.FirstOrDefaultAsync(p => p.UsernameSlug == slug, p => p.Prefix);
        if (profile is null)
            return Result<ProfileResponse>.Failure(ResponseMessages.ProfileNotFound);

        if (currentProfileId.HasValue && currentProfileId.Value != profile.Id)
        {
            var isBlocked = await _userBanRepo.FirstOrDefaultAsync(b =>
                b.BlockerProfileId == profile.Id && b.BlockedProfileId == currentProfileId.Value);
            if (isBlocked is not null)
                return Result<ProfileResponse>.Failure(ResponseMessages.ProfileNotFound);
        }

        bool isFollowing = false;
        if (currentProfileId.HasValue && currentProfileId.Value != profile.Id)
        {
            var follow = await _followRepo.FirstOrDefaultAsync(f =>
                f.FollowerId == currentProfileId.Value && f.FollowingId == profile.Id);
            isFollowing = follow is not null;
        }

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);
        return Result<ProfileResponse>.Success(BuildResponse(profile, prefixResponse, isFollowing));
    }

    public async Task<Result<ProfileResponse>> UpdateProfileAsync(Guid authUserId, string? displayName, string? bio, bool? isPrivate)
    {
        var profile = await _profileRepo.FirstOrDefaultAsync(p => p.AuthUserId == authUserId, p => p.Prefix);
        if (profile is null)
            return Result<ProfileResponse>.Failure(ResponseMessages.ProfileNotFound);

        if (displayName is not null) profile.DisplayName = displayName;
        if (bio is not null) profile.Bio = bio;
        if (isPrivate.HasValue) profile.IsPrivate = isPrivate.Value;

        profile.UpdatedAt = DateTime.UtcNow;
        _profileRepo.Update(profile);
        await _profileRepo.SaveChangesAsync();

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);
        return Result<ProfileResponse>.Success(BuildResponse(profile, prefixResponse));
    }

    public async Task<Result<ProfileResponse>> UpdateProfilePictureAsync(Guid authUserId, Stream fileStream, string fileName)
    {
        var profile = await _profileRepo.FirstOrDefaultAsync(p => p.AuthUserId == authUserId, p => p.Prefix);
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
        _profileRepo.Update(profile);
        await _profileRepo.SaveChangesAsync();

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);
        return Result<ProfileResponse>.Success(BuildResponse(profile, prefixResponse));
    }

    public async Task<Result<ProfileResponse>> RemoveProfilePictureAsync(Guid authUserId)
    {
        var profile = await _profileRepo.FirstOrDefaultAsync(p => p.AuthUserId == authUserId, p => p.Prefix);
        if (profile is null)
            return Result<ProfileResponse>.Failure(ResponseMessages.ProfileNotFound);

        if (profile.ProfilePicturePublicId is not null)
        {
            await _cloudinaryService.DeleteAsync(profile.ProfilePicturePublicId);
        }

        profile.ProfilePictureUrl = null;
        profile.ProfilePicturePublicId = null;
        profile.UpdatedAt = DateTime.UtcNow;
        _profileRepo.Update(profile);
        await _profileRepo.SaveChangesAsync();

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);
        return Result<ProfileResponse>.Success(BuildResponse(profile, prefixResponse));
    }

    public async Task<Result<ProfileResponse>> UpdateBannerAsync(Guid authUserId, Stream fileStream, string fileName)
    {
        var profile = await _profileRepo.FirstOrDefaultAsync(p => p.AuthUserId == authUserId, p => p.Prefix);
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
        _profileRepo.Update(profile);
        await _profileRepo.SaveChangesAsync();

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);
        return Result<ProfileResponse>.Success(BuildResponse(profile, prefixResponse));
    }

    public async Task<Result<ProfileResponse>> RemoveBannerAsync(Guid authUserId)
    {
        var profile = await _profileRepo.FirstOrDefaultAsync(p => p.AuthUserId == authUserId, p => p.Prefix);
        if (profile is null)
            return Result<ProfileResponse>.Failure(ResponseMessages.ProfileNotFound);

        if (profile.BannerPublicId is not null)
        {
            await _cloudinaryService.DeleteAsync(profile.BannerPublicId);
        }

        profile.BannerUrl = null;
        profile.BannerPublicId = null;
        profile.UpdatedAt = DateTime.UtcNow;
        _profileRepo.Update(profile);
        await _profileRepo.SaveChangesAsync();

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
            var blockerProfileIds = (await _userBanRepo.GetListAsync(b =>
                b.BlockedProfileId == currentProfileId.Value))
                .Select(b => b.BlockerProfileId);

            var blockedProfileIds = (await _userBanRepo.GetListAsync(b =>
                b.BlockerProfileId == currentProfileId.Value))
                .Select(b => b.BlockedProfileId);

            excludedProfileIds = blockerProfileIds.Concat(blockedProfileIds).ToHashSet();
        }

        var profiles = await _profileRepo.GetPagedAsync(
            p => (p.UsernameSlug.StartsWith(normalized) || p.DisplayName.ToLower().StartsWith(normalized))
                && !excludedProfileIds.Contains(p.Id),
            p => p.FollowersCount,
            skip,
            pageSize);

        var totalCount = await _profileRepo.CountAsync(
            p => (p.UsernameSlug.StartsWith(normalized) || p.DisplayName.ToLower().StartsWith(normalized))
                && !excludedProfileIds.Contains(p.Id));

        HashSet<Guid> followedIds = [];
        if (currentProfileId.HasValue)
        {
            var profileIds = profiles.Select(p => p.Id).ToList();
            var existingFollows = await _followRepo.GetListAsync(f =>
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
        var target = await _profileRepo.FirstOrDefaultAsync(p => p.UsernameSlug == slug);
        if (target is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        if (target.Id == moderatorProfileId)
            return Result.Failure(ResponseMessages.CannotBanYourself);

        if (target.IsBanned)
            return Result.Failure(ResponseMessages.UserAlreadyBanned);

        var adminRole = await _roleRepo.FirstOrDefaultAsync(r => r.Name == "admin");
        if (adminRole is not null)
        {
            var isAdmin = await _userRoleRepo.FirstOrDefaultAsync(ur =>
                ur.ProfileId == target.Id && ur.RoleId == adminRole.Id);
            if (isAdmin is not null)
                return Result.Failure(ResponseMessages.CannotBanAdmin);
        }

        target.IsBanned = true;
        target.BannedAt = DateTime.UtcNow;
        target.BannedByProfileId = moderatorProfileId;
        _profileRepo.Update(target);
        await _profileRepo.SaveChangesAsync();

        return Result.Success(ResponseMessages.BanSuccessful);
    }

    public async Task<Result> UnbanUserAsync(Guid moderatorProfileId, string username)
    {
        var slug = username.ToLowerInvariant();
        var target = await _profileRepo.FirstOrDefaultAsync(p => p.UsernameSlug == slug);
        if (target is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        if (!target.IsBanned)
            return Result.Failure(ResponseMessages.UserNotBanned);

        target.IsBanned = false;
        target.BannedAt = null;
        target.BannedByProfileId = null;
        _profileRepo.Update(target);
        await _profileRepo.SaveChangesAsync();

        return Result.Success(ResponseMessages.UnbanSuccessful);
    }

    public async Task<Result> BlockUserAsync(Guid blockerProfileId, string username)
    {
        var slug = username.ToLowerInvariant();
        var target = await _profileRepo.FirstOrDefaultAsync(p => p.UsernameSlug == slug);
        if (target is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        if (target.Id == blockerProfileId)
            return Result.Failure(ResponseMessages.CannotBlockYourself);

        var existingBan = await _userBanRepo.FirstOrDefaultAsync(b =>
            b.BlockerProfileId == blockerProfileId && b.BlockedProfileId == target.Id);
        if (existingBan is not null)
            return Result.Failure(ResponseMessages.AlreadyBlocked);

        var blockedByTarget = await _userBanRepo.FirstOrDefaultAsync(b =>
            b.BlockerProfileId == target.Id && b.BlockedProfileId == blockerProfileId);
        if (blockedByTarget is not null)
            return Result.Failure(ResponseMessages.BlockedByUser);

        var followToTarget = await _followRepo.FirstOrDefaultAsync(f =>
            f.FollowerId == blockerProfileId && f.FollowingId == target.Id);
        if (followToTarget is not null)
        {
            await _followRepo.DeleteAsync(followToTarget.Id);
            target.FollowersCount = Math.Max(0, target.FollowersCount - 1);
            target.UpdatedAt = DateTime.UtcNow;
            _profileRepo.Update(target);
            await _profileRepo.SaveChangesAsync();

            var blockerProfile = await _profileRepo.GetByIdAsync(blockerProfileId);
            if (blockerProfile is not null)
            {
                blockerProfile.FollowingCount = Math.Max(0, blockerProfile.FollowingCount - 1);
                blockerProfile.UpdatedAt = DateTime.UtcNow;
                _profileRepo.Update(blockerProfile);
                await _profileRepo.SaveChangesAsync();
            }
        }

        var followFromTarget = await _followRepo.FirstOrDefaultAsync(f =>
            f.FollowerId == target.Id && f.FollowingId == blockerProfileId);
        if (followFromTarget is not null)
        {
            await _followRepo.DeleteAsync(followFromTarget.Id);
            target.FollowingCount = Math.Max(0, target.FollowingCount - 1);
            target.UpdatedAt = DateTime.UtcNow;
            _profileRepo.Update(target);
            await _profileRepo.SaveChangesAsync();

            var blockerProfile = await _profileRepo.GetByIdAsync(blockerProfileId);
            if (blockerProfile is not null)
            {
                blockerProfile.FollowersCount = Math.Max(0, blockerProfile.FollowersCount - 1);
                blockerProfile.UpdatedAt = DateTime.UtcNow;
                _profileRepo.Update(blockerProfile);
                await _profileRepo.SaveChangesAsync();
            }
        }

        var ban = new UserBan
        {
            Id = Guid.NewGuid(),
            BlockerProfileId = blockerProfileId,
            BlockedProfileId = target.Id,
            CreatedAt = DateTime.UtcNow,
        };

        await _userBanRepo.CreateAsync(ban);

        var blockerPostIds = await _postRepo.GetListAsync(p => p.ProfileId == blockerProfileId);
        var blockerPostIdHashes = blockerPostIds.Select(p => p.Id).ToHashSet();

        if (blockerPostIdHashes.Count > 0)
        {
            var blockedComments = await _commentRepo.GetListAsync(c =>
                c.ProfileId == target.Id && blockerPostIdHashes.Contains(c.PostId));

            foreach (var comment in blockedComments)
            {
                comment.IsActive = false;
                _commentRepo.Update(comment);

                var post = blockerPostIds.FirstOrDefault(p => p.Id == comment.PostId);
                if (post is not null)
                {
                    post.CommentCount = Math.Max(0, post.CommentCount - 1);
                    _postRepo.Update(post);
                }

                if (comment.ParentCommentId.HasValue)
                {
                    var parentComment = await _commentRepo.GetByIdAsync(comment.ParentCommentId.Value);
                    if (parentComment is not null)
                    {
                        parentComment.ReplyCount = Math.Max(0, parentComment.ReplyCount - 1);
                        _commentRepo.Update(parentComment);
                    }
                }
            }

            if (blockedComments.Count > 0)
                await _commentRepo.SaveChangesAsync();
        }

        return Result.Success(ResponseMessages.BlockSuccessful);
    }

    public async Task<Result> UnblockUserAsync(Guid blockerProfileId, string username)
    {
        var slug = username.ToLowerInvariant();
        var target = await _profileRepo.FirstOrDefaultAsync(p => p.UsernameSlug == slug);
        if (target is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        var ban = await _userBanRepo.FirstOrDefaultAsync(b =>
            b.BlockerProfileId == blockerProfileId && b.BlockedProfileId == target.Id);
        if (ban is null)
            return Result.Failure(ResponseMessages.NotBlocked);

        await _userBanRepo.DeleteAsync(ban.Id);
        return Result.Success(ResponseMessages.UnblockSuccessful);
    }

    public async Task<Result<PagedResult<BlockedUserResponse>>> GetBlockedUsersAsync(
        Guid profileId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        var bans = await _userBanRepo.GetPagedAsync(
            b => b.BlockerProfileId == profileId,
            b => b.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _userBanRepo.CountAsync(b => b.BlockerProfileId == profileId);

        var blockedProfileIds = bans.Select(b => b.BlockedProfileId).Distinct().ToList();
        var profiles = blockedProfileIds.Count > 0
            ? await _profileRepo.GetListAsync(p => blockedProfileIds.Contains(p.Id))
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

        var prefix = await _prefixRepo.GetByIdAsync(prefixId.Value);
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
