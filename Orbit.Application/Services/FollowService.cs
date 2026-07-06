using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.Entities;
using Orbit.Domain.DataBase;

namespace Orbit.Application.Services;

public class FollowService : IFollowService
{
    private readonly IUnitOfWork _uow;

    public FollowService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result> FollowUserAsync(Guid followerProfileId, string username)
    {
        var slug = username.ToLowerInvariant();
        var targetProfile = await _uow.ProfileRepository.Get(p => p.UsernameSlug == slug);
        if (targetProfile is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        if (targetProfile.Id == followerProfileId)
            return Result.Failure(ResponseMessages.CannotFollowYourself);

        var blockedByTarget = await _uow.UserBanRepository.Get(b =>
            b.BlockerProfileId == targetProfile.Id && b.BlockedProfileId == followerProfileId);
        if (blockedByTarget is not null)
            return Result.Failure(ResponseMessages.CannotFollowBlockedByUser);

        var followerBlockedTarget = await _uow.UserBanRepository.Get(b =>
            b.BlockerProfileId == followerProfileId && b.BlockedProfileId == targetProfile.Id);
        if (followerBlockedTarget is not null)
            return Result.Failure(ResponseMessages.CannotFollowBlockedUser);

        var existingFollow = await _uow.FollowRepository.Get(f =>
            f.FollowerId == followerProfileId && f.FollowingId == targetProfile.Id);
        if (existingFollow is not null)
            return Result.Failure(ResponseMessages.AlreadyFollowing);

        var follow = new Follow
        {
            Id = Guid.NewGuid(),
            FollowerId = followerProfileId,
            FollowingId = targetProfile.Id,
            CreatedAt = DateTime.UtcNow,
        };

        await _uow.FollowRepository.Create(follow);

        targetProfile.FollowersCount++;
        targetProfile.UpdatedAt = DateTime.UtcNow;
        await _uow.ProfileRepository.Update(targetProfile);
        await _uow.SaveChangesAsync();

        var followerProfile = await _uow.ProfileRepository.Get(p => p.Id == followerProfileId);
        if (followerProfile is not null)
        {
            followerProfile.FollowingCount++;
            followerProfile.UpdatedAt = DateTime.UtcNow;
            await _uow.ProfileRepository.Update(followerProfile);
            await _uow.SaveChangesAsync();
        }

        return Result.Success(ResponseMessages.FollowSuccessful);
    }

    public async Task<Result> UnfollowUserAsync(Guid followerProfileId, string username)
    {
        var slug = username.ToLowerInvariant();
        var targetProfile = await _uow.ProfileRepository.Get(p => p.UsernameSlug == slug);
        if (targetProfile is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        var follow = await _uow.FollowRepository.Get(f =>
            f.FollowerId == followerProfileId && f.FollowingId == targetProfile.Id);
        if (follow is null)
            return Result.Failure(ResponseMessages.NotFollowing);

        await _uow.FollowRepository.Delete(follow);

        targetProfile.FollowersCount = Math.Max(0, targetProfile.FollowersCount - 1);
        targetProfile.UpdatedAt = DateTime.UtcNow;
        await _uow.ProfileRepository.Update(targetProfile);
        await _uow.SaveChangesAsync();

        var followerProfile = await _uow.ProfileRepository.Get(p => p.Id == followerProfileId);
        if (followerProfile is not null)
        {
            followerProfile.FollowingCount = Math.Max(0, followerProfile.FollowingCount - 1);
            followerProfile.UpdatedAt = DateTime.UtcNow;
            await _uow.ProfileRepository.Update(followerProfile);
            await _uow.SaveChangesAsync();
        }

        return Result.Success(ResponseMessages.UnfollowSuccessful);
    }

    public async Task<Result<PagedResult<PostAuthorResponse>>> GetFollowersAsync(
        string username, Guid? currentProfileId, int page, int pageSize)
    {
        var slug = username.ToLowerInvariant();
        var profile = await _uow.ProfileRepository.Get(p => p.UsernameSlug == slug);
        if (profile is null)
            return Result<PagedResult<PostAuthorResponse>>.Failure(ResponseMessages.ProfileNotFound);

        var skip = (page - 1) * pageSize;
        var followers = await _uow.FollowRepository.GetPagedAsync(
            f => f.FollowingId == profile.Id,
            f => f.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _uow.FollowRepository.CountAsync(f => f.FollowingId == profile.Id);

        var items = await BuildAuthorResponseList(followers, f => f.FollowerId, currentProfileId);
        return Result<PagedResult<PostAuthorResponse>>.Success(new PagedResult<PostAuthorResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    public async Task<Result<PagedResult<PostAuthorResponse>>> GetFollowingAsync(
        string username, Guid? currentProfileId, int page, int pageSize)
    {
        var slug = username.ToLowerInvariant();
        var profile = await _uow.ProfileRepository.Get(p => p.UsernameSlug == slug);
        if (profile is null)
            return Result<PagedResult<PostAuthorResponse>>.Failure(ResponseMessages.ProfileNotFound);

        var skip = (page - 1) * pageSize;
        var following = await _uow.FollowRepository.GetPagedAsync(
            f => f.FollowerId == profile.Id,
            f => f.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _uow.FollowRepository.CountAsync(f => f.FollowerId == profile.Id);

        var items = await BuildAuthorResponseList(following, f => f.FollowingId, currentProfileId);
        return Result<PagedResult<PostAuthorResponse>>.Success(new PagedResult<PostAuthorResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    private async Task<List<PostAuthorResponse>> BuildAuthorResponseList(
        List<Follow> follows, Func<Follow, Guid> profileIdSelector, Guid? currentProfileId = null)
    {
        var profileIds = follows.Select(profileIdSelector).Distinct().ToList();
        var profiles = profileIds.Count > 0
            ? await _uow.ProfileRepository.GetListAsync(p => profileIds.Contains(p.Id))
            : [];
        var profileMap = profiles.ToDictionary(p => p.Id);

        HashSet<Guid> followedIds = [];
        if (currentProfileId.HasValue)
        {
            var existingFollows = await _uow.FollowRepository.GetListAsync(f =>
                f.FollowerId == currentProfileId.Value && profileIds.Contains(f.FollowingId));
            followedIds = existingFollows.Select(f => f.FollowingId).ToHashSet();
        }

        return follows
            .Select(f =>
            {
                var pid = profileIdSelector(f);
                var p = profileMap.GetValueOrDefault(pid);
                var isFollowing = currentProfileId.HasValue && followedIds.Contains(pid);
                return p is not null
                    ? new PostAuthorResponse(p.Id, p.Username, p.DisplayName, p.ProfilePictureUrl, isFollowing)
                    : new PostAuthorResponse(pid, "Unknown", "Unknown", null, false);
            })
            .ToList();
    }
}
