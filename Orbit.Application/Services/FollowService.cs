using Orbit.Application.Common;
using Orbit.Application.Helpers;
using Orbit.Application.Constants;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Models.Responses;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.Entities;
using Orbit.Domain.DataBase;
using Orbit.Domain.Exceptions;

namespace Orbit.Application.Services;

public class FollowService : IFollowService
{
    private readonly IUnitOfWork _uow;

    public FollowService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<GenericResponse<string>> FollowUserAsync(Guid followerProfileId, string username)
    {
        var slug = username.ToLowerInvariant();
        var targetProfile = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (targetProfile is null)
            throw new NotFoundException(ResponseMessages.ProfileNotFound);

        if (targetProfile.Id == followerProfileId)
            throw new BadRequestException(ResponseMessages.CannotFollowYourself);

        var blockedByTarget = await _uow.userBanRepository.Get(b =>
            b.BlockerProfileId == targetProfile.Id && b.BlockedProfileId == followerProfileId);
        if (blockedByTarget is not null)
            throw new BadRequestException(ResponseMessages.CannotFollowBlockedByUser);

        var followerBlockedTarget = await _uow.userBanRepository.Get(b =>
            b.BlockerProfileId == followerProfileId && b.BlockedProfileId == targetProfile.Id);
        if (followerBlockedTarget is not null)
            throw new BadRequestException(ResponseMessages.CannotFollowBlockedUser);

        var existingFollow = await _uow.followRepository.Get(f =>
            f.FollowerId == followerProfileId && f.FollowingId == targetProfile.Id);
        if (existingFollow is not null)
            throw new BadRequestException(ResponseMessages.AlreadyFollowing);

        var follow = new Follow
        {
            Id = Guid.NewGuid(),
            FollowerId = followerProfileId,
            FollowingId = targetProfile.Id,
            CreatedAt = DateTime.UtcNow,
        };

        await _uow.followRepository.Create(follow);

        targetProfile.FollowersCount++;
        targetProfile.UpdatedAt = DateTime.UtcNow;
        await _uow.profileRepository.Update(targetProfile);
        await _uow.SaveChangesAsync();

        var followerProfile = await _uow.profileRepository.Get(p => p.Id == followerProfileId);
        if (followerProfile is not null)
        {
            followerProfile.FollowingCount++;
            followerProfile.UpdatedAt = DateTime.UtcNow;
            await _uow.profileRepository.Update(followerProfile);
            await _uow.SaveChangesAsync();
        }

        return ResponseHelper.Create<string>(default, message: ResponseMessages.FollowSuccessful);
    }

    public async Task<GenericResponse<string>> UnfollowUserAsync(Guid followerProfileId, string username)
    {
        var slug = username.ToLowerInvariant();
        var targetProfile = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (targetProfile is null)
            throw new NotFoundException(ResponseMessages.ProfileNotFound);

        var follow = await _uow.followRepository.Get(f =>
            f.FollowerId == followerProfileId && f.FollowingId == targetProfile.Id);
        if (follow is null)
            throw new BadRequestException(ResponseMessages.NotFollowing);

        await _uow.followRepository.Delete(follow);

        targetProfile.FollowersCount = Math.Max(0, targetProfile.FollowersCount - 1);
        targetProfile.UpdatedAt = DateTime.UtcNow;
        await _uow.profileRepository.Update(targetProfile);
        await _uow.SaveChangesAsync();

        var followerProfile = await _uow.profileRepository.Get(p => p.Id == followerProfileId);
        if (followerProfile is not null)
        {
            followerProfile.FollowingCount = Math.Max(0, followerProfile.FollowingCount - 1);
            followerProfile.UpdatedAt = DateTime.UtcNow;
            await _uow.profileRepository.Update(followerProfile);
            await _uow.SaveChangesAsync();
        }

        return ResponseHelper.Create<string>(default, message: ResponseMessages.UnfollowSuccessful);
    }

    public async Task<GenericResponse<PagedResult<PostAuthorDto>>> GetFollowersAsync(
        string username, Guid? currentProfileId, int page, int pageSize)
    {
        var slug = username.ToLowerInvariant();
        var profile = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (profile is null)
            throw new NotFoundException(ResponseMessages.ProfileNotFound);

        var skip = (page - 1) * pageSize;
        var followers = await _uow.followRepository.GetPagedAsync(
            f => f.FollowingId == profile.Id,
            f => f.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _uow.followRepository.CountAsync(f => f.FollowingId == profile.Id);

        var items = await BuildAuthorResponseList(followers, f => f.FollowerId, currentProfileId);
        return ResponseHelper.Create(new PagedResult<PostAuthorDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    public async Task<GenericResponse<PagedResult<PostAuthorDto>>> GetFollowingAsync(
        string username, Guid? currentProfileId, int page, int pageSize)
    {
        var slug = username.ToLowerInvariant();
        var profile = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (profile is null)
            throw new NotFoundException(ResponseMessages.ProfileNotFound);

        var skip = (page - 1) * pageSize;
        var following = await _uow.followRepository.GetPagedAsync(
            f => f.FollowerId == profile.Id,
            f => f.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _uow.followRepository.CountAsync(f => f.FollowerId == profile.Id);

        var items = await BuildAuthorResponseList(following, f => f.FollowingId, currentProfileId);
        return ResponseHelper.Create(new PagedResult<PostAuthorDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    private async Task<List<PostAuthorDto>> BuildAuthorResponseList(
        List<Follow> follows, Func<Follow, Guid> profileIdSelector, Guid? currentProfileId = null)
    {
        var profileIds = follows.Select(profileIdSelector).Distinct().ToList();
        var profiles = profileIds.Count > 0
            ? await _uow.profileRepository.GetListAsync(p => profileIds.Contains(p.Id))
            : [];
        var profileMap = profiles.ToDictionary(p => p.Id);

        HashSet<Guid> followedIds = [];
        if (currentProfileId.HasValue)
        {
            var existingFollows = await _uow.followRepository.GetListAsync(f =>
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
                    ? new PostAuthorDto(p.Id, p.Username, p.DisplayName, p.ProfilePictureUrl, isFollowing)
                    : new PostAuthorDto(pid, "Unknown", "Unknown", null, false);
            })
            .ToList();
    }
}
