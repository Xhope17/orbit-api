using System.Text.RegularExpressions;
using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Enums;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;

namespace Orbit.Application.Services;

public class CommunityService : ICommunityService
{
    private readonly IGenericRepository<Community> _communityRepo;
    private readonly IGenericRepository<CommunityMember> _memberRepo;
    private readonly IGenericRepository<Profile> _profileRepo;
    private readonly IGenericRepository<CommunityJoinRequest> _joinRequestRepo;
    private readonly IGenericRepository<CommunityInvitation> _invitationRepo;
    private readonly IGenericRepository<Post> _postRepo;
    private readonly IGenericRepository<PostMedia> _mediaRepo;
    private readonly IGenericRepository<PostLike> _likeRepo;
    private readonly IGenericRepository<SavedPost> _savedPostRepo;
    private readonly ICloudinaryService _cloudinaryService;

    public CommunityService(
        IGenericRepository<Community> communityRepo,
        IGenericRepository<CommunityMember> memberRepo,
        IGenericRepository<Profile> profileRepo,
        IGenericRepository<CommunityJoinRequest> joinRequestRepo,
        IGenericRepository<CommunityInvitation> invitationRepo,
        IGenericRepository<Post> postRepo,
        IGenericRepository<PostMedia> mediaRepo,
        IGenericRepository<PostLike> likeRepo,
        IGenericRepository<SavedPost> savedPostRepo,
        ICloudinaryService cloudinaryService)
    {
        _communityRepo = communityRepo;
        _memberRepo = memberRepo;
        _profileRepo = profileRepo;
        _joinRequestRepo = joinRequestRepo;
        _invitationRepo = invitationRepo;
        _postRepo = postRepo;
        _mediaRepo = mediaRepo;
        _likeRepo = likeRepo;
        _savedPostRepo = savedPostRepo;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<Result<CommunityResponse>> CreateCommunityAsync(Guid authUserId, string name, string? description, bool isPrivate)
    {
        var profile = await _profileRepo.FirstOrDefaultAsync(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result<CommunityResponse>.Failure(ResponseMessages.ProfileNotFound);

        var slug = GenerateSlug(name);

        var existingSlug = await _communityRepo.FirstOrDefaultAsync(c => c.Slug == slug);
        if (existingSlug is not null)
            return Result<CommunityResponse>.Failure(ResponseMessages.SlugAlreadyTaken);

        var community = new Community
        {
            Id = Guid.NewGuid(),
            OwnerProfileId = profile.Id,
            Name = name.Trim(),
            Slug = slug,
            Description = description?.Trim(),
            IsPrivate = isPrivate,
            MemberCount = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _communityRepo.CreateAsync(community);

        var ownerMember = new CommunityMember
        {
            Id = Guid.NewGuid(),
            CommunityId = community.Id,
            ProfileId = profile.Id,
            Role = "owner",
            JoinedAt = DateTime.UtcNow,
        };

        await _memberRepo.CreateAsync(ownerMember);

        var response = BuildCommunityResponse(community, profile, isMember: true, memberRole: "owner", hasPendingJoinRequest: false, hasPendingInvitation: false);
        return Result<CommunityResponse>.Success(response, ResponseMessages.CommunityCreated);
    }

    public async Task<Result<CommunityResponse>> UpdateCommunityAsync(Guid authUserId, string slug, string? name, string? description, bool? isPrivate)
    {
        var profile = await _profileRepo.FirstOrDefaultAsync(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result<CommunityResponse>.Failure(ResponseMessages.ProfileNotFound);

        var community = await _communityRepo.FirstOrDefaultAsync(c => c.Slug == slug);
        if (community is null)
            return Result<CommunityResponse>.Failure(ResponseMessages.CommunityNotFound);

        var membership = await _memberRepo.FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.ProfileId == profile.Id);
        if (membership is null || (membership.Role != "owner" && membership.Role != "co_leader"))
            return Result<CommunityResponse>.Failure(ResponseMessages.NoPermission);

        if (name is not null && name.Trim() != community.Name)
        {
            var newSlug = GenerateSlug(name);
            var existingSlug = await _communityRepo.FirstOrDefaultAsync(c => c.Slug == newSlug && c.Id != community.Id);
            if (existingSlug is not null)
                return Result<CommunityResponse>.Failure(ResponseMessages.SlugAlreadyTaken);

            community.Name = name.Trim();
            community.Slug = newSlug;
        }

        if (description is not null)
            community.Description = description.Trim();

        if (isPrivate.HasValue)
            community.IsPrivate = isPrivate.Value;

        community.UpdatedAt = DateTime.UtcNow;
        _communityRepo.Update(community);
        await _communityRepo.SaveChangesAsync();

        var ownerProfile = await _profileRepo.GetByIdAsync(community.OwnerProfileId);
        var ownerResponse = BuildOwnerResponse(ownerProfile!);
        var currentRole = membership.Role;

        var response = new CommunityResponse(
            community.Id,
            community.Name,
            community.Slug,
            community.Description,
            community.MemberCount,
            community.IsPrivate,
            community.BannerUrl,
            community.IconUrl,
            ownerResponse,
            true,
            currentRole,
            HasPendingJoinRequest: false,
            HasPendingInvitation: false,
            community.CreatedAt
        );

        return Result<CommunityResponse>.Success(response, ResponseMessages.CommunityUpdated);
    }

    public async Task<Result> DeleteCommunityAsync(Guid authUserId, string slug)
    {
        var profile = await _profileRepo.FirstOrDefaultAsync(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        var community = await _communityRepo.FirstOrDefaultAsync(c => c.Slug == slug);
        if (community is null)
            return Result.Failure(ResponseMessages.CommunityNotFound);

        if (community.OwnerProfileId != profile.Id)
            return Result.Failure(ResponseMessages.NoPermission);

        await _communityRepo.DeleteAsync(community.Id);
        return Result.Success(ResponseMessages.CommunityDeleted);
    }

    public async Task<Result<CommunityResponse>> GetCommunityAsync(string slug, Guid? currentProfileId)
    {
        var community = await _communityRepo.FirstOrDefaultAsync(c => c.Slug == slug);
        if (community is null)
            return Result<CommunityResponse>.Failure(ResponseMessages.CommunityNotFound);

        var owner = await _profileRepo.GetByIdAsync(community.OwnerProfileId);
        if (owner is null)
            return Result<CommunityResponse>.Failure(ResponseMessages.ProfileNotFound);

        var isMember = false;
        string? memberRole = null;
        bool? hasPendingJoinRequest = null;
        bool? hasPendingInvitation = null;

        if (currentProfileId.HasValue)
        {
            var membership = await _memberRepo.FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.ProfileId == currentProfileId.Value);
            if (membership is not null)
            {
                isMember = true;
                memberRole = membership.Role;
            }

            hasPendingJoinRequest = await _joinRequestRepo.CountAsync(
                jr => jr.CommunityId == community.Id
                   && jr.ProfileId == currentProfileId.Value
                   && jr.Status == "pending") > 0;

            hasPendingInvitation = await _invitationRepo.CountAsync(
                inv => inv.CommunityId == community.Id
                    && inv.ProfileId == currentProfileId.Value
                    && inv.Status == "pending") > 0;
        }

        if (community.IsPrivate && !isMember)
            return Result<CommunityResponse>.Failure(ResponseMessages.CannotJoinPrivate);

        var response = BuildCommunityResponse(community, owner, isMember, memberRole, hasPendingJoinRequest, hasPendingInvitation);
        return Result<CommunityResponse>.Success(response);
    }

    public async Task<Result<PagedResult<CommunitySummaryResponse>>> SearchCommunitiesAsync(string? query, int page, int pageSize, Guid? currentProfileId = null)
    {
        var skip = (page - 1) * pageSize;

        List<Community> communities;
        int totalCount;

        if (string.IsNullOrWhiteSpace(query))
        {
            communities = await _communityRepo.GetPagedAsync(
                _ => true,
                c => c.MemberCount,
                skip,
                pageSize);
            totalCount = await _communityRepo.CountAsync(_ => true);
        }
        else
        {
            communities = await _communityRepo.GetPagedAsync(
                c => c.Name.Contains(query),
                c => c.MemberCount,
                skip,
                pageSize);
            totalCount = await _communityRepo.CountAsync(c => c.Name.Contains(query));
        }

        Dictionary<Guid, bool>? pendingRequests = null;
        Dictionary<Guid, bool>? pendingInvitations = null;

        if (currentProfileId.HasValue)
        {
            var communityIds = communities.Select(c => c.Id).ToList();

            var requests = await _joinRequestRepo.GetListAsync(
                jr => communityIds.Contains(jr.CommunityId)
                   && jr.ProfileId == currentProfileId.Value
                   && jr.Status == "pending");
            pendingRequests = requests.ToDictionary(r => r.CommunityId, _ => true);

            var invitations = await _invitationRepo.GetListAsync(
                inv => communityIds.Contains(inv.CommunityId)
                    && inv.ProfileId == currentProfileId.Value
                    && inv.Status == "pending");
            pendingInvitations = invitations.ToDictionary(inv => inv.CommunityId, _ => true);
        }

        var items = communities.Select(c => new CommunitySummaryResponse(
            c.Id,
            c.Name,
            c.Slug,
            c.Description,
            c.MemberCount,
            c.IsPrivate,
            c.IconUrl,
            pendingRequests?.ContainsKey(c.Id),
            pendingInvitations?.ContainsKey(c.Id)
        )).ToList();

        return Result<PagedResult<CommunitySummaryResponse>>.Success(new PagedResult<CommunitySummaryResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    public async Task<Result<PagedResult<CommunitySummaryResponse>>> GetMyCommunitiesAsync(Guid profileId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;

        var memberships = await _memberRepo.GetListAsync(m => m.ProfileId == profileId);
        var communityIds = memberships.Select(m => m.CommunityId).ToList();

        if (communityIds.Count == 0)
        {
            return Result<PagedResult<CommunitySummaryResponse>>.Success(new PagedResult<CommunitySummaryResponse>
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
            });
        }

        var communities = await _communityRepo.GetListAsync(c => communityIds.Contains(c.Id));

        var totalCount = communities.Count;
        var ordered = communities.OrderByDescending(c => c.MemberCount).Skip(skip).Take(pageSize).ToList();

        var items = ordered.Select(c => new CommunitySummaryResponse(
            c.Id,
            c.Name,
            c.Slug,
            c.Description,
            c.MemberCount,
            c.IsPrivate,
            c.IconUrl,
            HasPendingJoinRequest: null,
            HasPendingInvitation: null
        )).ToList();

        return Result<PagedResult<CommunitySummaryResponse>>.Success(new PagedResult<CommunitySummaryResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    public async Task<Result> JoinCommunityAsync(Guid profileId, string slug)
    {
        var community = await _communityRepo.FirstOrDefaultAsync(c => c.Slug == slug);
        if (community is null)
            return Result.Failure(ResponseMessages.CommunityNotFound);

        if (community.IsPrivate)
            return Result.Failure(ResponseMessages.CannotJoinPrivate);

        var existingMember = await _memberRepo.FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.ProfileId == profileId);
        if (existingMember is not null)
            return Result.Failure(ResponseMessages.AlreadyMember);

        var member = new CommunityMember
        {
            Id = Guid.NewGuid(),
            CommunityId = community.Id,
            ProfileId = profileId,
            Role = "member",
            JoinedAt = DateTime.UtcNow,
        };

        await _memberRepo.CreateAsync(member);

        community.MemberCount++;
        community.UpdatedAt = DateTime.UtcNow;
        _communityRepo.Update(community);
        await _communityRepo.SaveChangesAsync();

        return Result.Success(ResponseMessages.JoinSuccessful);
    }

    public async Task<Result> LeaveCommunityAsync(Guid profileId, string slug)
    {
        var community = await _communityRepo.FirstOrDefaultAsync(c => c.Slug == slug);
        if (community is null)
            return Result.Failure(ResponseMessages.CommunityNotFound);

        var membership = await _memberRepo.FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.ProfileId == profileId);
        if (membership is null)
            return Result.Failure(ResponseMessages.NotMember);

        if (membership.Role == "owner")
            return Result.Failure(ResponseMessages.OwnerCannotLeave);

        await _memberRepo.DeleteAsync(membership.Id);

        community.MemberCount = Math.Max(0, community.MemberCount - 1);
        community.UpdatedAt = DateTime.UtcNow;
        _communityRepo.Update(community);
        await _communityRepo.SaveChangesAsync();

        return Result.Success(ResponseMessages.LeaveSuccessful);
    }

    public async Task<Result> KickMemberAsync(Guid authUserId, string slug, Guid targetProfileId)
    {
        var profile = await _profileRepo.FirstOrDefaultAsync(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        var community = await _communityRepo.FirstOrDefaultAsync(c => c.Slug == slug);
        if (community is null)
            return Result.Failure(ResponseMessages.CommunityNotFound);

        var actorMembership = await _memberRepo.FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.ProfileId == profile.Id);
        if (actorMembership is null || (actorMembership.Role != "owner" && actorMembership.Role != "co_leader"))
            return Result.Failure(ResponseMessages.NoPermission);

        var targetMembership = await _memberRepo.FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.ProfileId == targetProfileId);
        if (targetMembership is null)
            return Result.Failure(ResponseMessages.NotMember);

        if (targetMembership.Role == "owner")
            return Result.Failure(ResponseMessages.CannotKickOwner);

        if (actorMembership.Role == "co_leader" && targetMembership.Role == "co_leader")
            return Result.Failure(ResponseMessages.NoPermission);

        await _memberRepo.DeleteAsync(targetMembership.Id);

        community.MemberCount = Math.Max(0, community.MemberCount - 1);
        community.UpdatedAt = DateTime.UtcNow;
        _communityRepo.Update(community);
        await _communityRepo.SaveChangesAsync();

        return Result.Success(ResponseMessages.MemberKicked);
    }

    public async Task<Result> AssignCoLeaderAsync(Guid authUserId, string slug, string targetUsername)
    {
        var profile = await _profileRepo.FirstOrDefaultAsync(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        var community = await _communityRepo.FirstOrDefaultAsync(c => c.Slug == slug);
        if (community is null)
            return Result.Failure(ResponseMessages.CommunityNotFound);

        if (community.OwnerProfileId != profile.Id)
            return Result.Failure(ResponseMessages.NoPermission);

        var targetProfile = await _profileRepo.FirstOrDefaultAsync(p => p.Username == targetUsername);
        if (targetProfile is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        var membership = await _memberRepo.FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.ProfileId == targetProfile.Id);
        if (membership is null)
            return Result.Failure(ResponseMessages.NotMember);

        if (membership.Role == "co_leader")
            return Result.Failure(ResponseMessages.AlreadyCoLeader);

        if (membership.Role == "owner")
            return Result.Failure(ResponseMessages.CannotKickOwner);

        membership.Role = "co_leader";
        _memberRepo.Update(membership);
        await _memberRepo.SaveChangesAsync();

        return Result.Success(ResponseMessages.CoLeaderAssigned);
    }

    public async Task<Result> RemoveCoLeaderAsync(Guid authUserId, string slug, string targetUsername)
    {
        var profile = await _profileRepo.FirstOrDefaultAsync(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        var community = await _communityRepo.FirstOrDefaultAsync(c => c.Slug == slug);
        if (community is null)
            return Result.Failure(ResponseMessages.CommunityNotFound);

        if (community.OwnerProfileId != profile.Id)
            return Result.Failure(ResponseMessages.NoPermission);

        var targetProfile = await _profileRepo.FirstOrDefaultAsync(p => p.Username == targetUsername);
        if (targetProfile is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        var membership = await _memberRepo.FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.ProfileId == targetProfile.Id);
        if (membership is null || membership.Role != "co_leader")
            return Result.Failure(ResponseMessages.NotCoLeader);

        membership.Role = "member";
        _memberRepo.Update(membership);
        await _memberRepo.SaveChangesAsync();

        return Result.Success(ResponseMessages.CoLeaderRemoved);
    }

    public async Task<Result<PagedResult<CommunityMemberResponse>>> GetMembersAsync(string slug, int page, int pageSize, Guid? currentProfileId)
    {
        var community = await _communityRepo.FirstOrDefaultAsync(c => c.Slug == slug);
        if (community is null)
            return Result<PagedResult<CommunityMemberResponse>>.Failure(ResponseMessages.CommunityNotFound);

        if (community.IsPrivate && currentProfileId.HasValue)
        {
            var isMember = await _memberRepo.FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.ProfileId == currentProfileId.Value);
            if (isMember is null)
                return Result<PagedResult<CommunityMemberResponse>>.Failure(ResponseMessages.CannotJoinPrivate);
        }
        else if (community.IsPrivate && !currentProfileId.HasValue)
        {
            return Result<PagedResult<CommunityMemberResponse>>.Failure(ResponseMessages.PrivateCommunityRequiresAuth);
        }

        var skip = (page - 1) * pageSize;

        var members = await _memberRepo.GetPagedAsync(
            m => m.CommunityId == community.Id,
            m => m.JoinedAt,
            skip,
            pageSize);

        var totalCount = await _memberRepo.CountAsync(m => m.CommunityId == community.Id);

        if (members.Count == 0)
        {
            return Result<PagedResult<CommunityMemberResponse>>.Success(new PagedResult<CommunityMemberResponse>
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
            });
        }

        var profileIds = members.Select(m => m.ProfileId).ToList();
        var profiles = await _profileRepo.GetListAsync(p => profileIds.Contains(p.Id));
        var profileMap = profiles.ToDictionary(p => p.Id);

        var items = members.Select(m =>
        {
            var p = profileMap.GetValueOrDefault(m.ProfileId);
            return new CommunityMemberResponse(
                m.ProfileId,
                p?.Username ?? "unknown",
                p?.DisplayName ?? "Unknown",
                p?.ProfilePictureUrl,
                m.Role,
                m.JoinedAt
            );
        }).ToList();

        return Result<PagedResult<CommunityMemberResponse>>.Success(new PagedResult<CommunityMemberResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    public async Task<Result<CommunityJoinRequestResponse>> RequestJoinAsync(Guid profileId, string slug)
    {
        var community = await _communityRepo.FirstOrDefaultAsync(c => c.Slug == slug);
        if (community is null)
            return Result<CommunityJoinRequestResponse>.Failure(ResponseMessages.CommunityNotFound);

        if (!community.IsPrivate)
            return Result<CommunityJoinRequestResponse>.Failure(ResponseMessages.AlreadyMember);

        var existingMember = await _memberRepo.FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.ProfileId == profileId);
        if (existingMember is not null)
            return Result<CommunityJoinRequestResponse>.Failure(ResponseMessages.AlreadyMember);

        var pending = await _joinRequestRepo.FirstOrDefaultAsync(r =>
            r.CommunityId == community.Id && r.ProfileId == profileId && r.Status == "pending");
        if (pending is not null)
            return Result<CommunityJoinRequestResponse>.Failure(ResponseMessages.JoinRequestAlreadyPending);

        var request = new CommunityJoinRequest
        {
            Id = Guid.NewGuid(),
            CommunityId = community.Id,
            ProfileId = profileId,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
        };

        await _joinRequestRepo.CreateAsync(request);

        var profile = await _profileRepo.GetByIdAsync(profileId);

        var response = new CommunityJoinRequestResponse(
            request.Id,
            profileId,
            profile?.Username ?? "unknown",
            profile?.DisplayName ?? "Unknown",
            profile?.ProfilePictureUrl,
            "pending",
            request.CreatedAt
        );

        return Result<CommunityJoinRequestResponse>.Success(response, ResponseMessages.JoinRequestSent);
    }

    public async Task<Result<PagedResult<CommunityJoinRequestResponse>>> GetJoinRequestsAsync(Guid authUserId, string slug, int page, int pageSize)
    {
        var profile = await _profileRepo.FirstOrDefaultAsync(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result<PagedResult<CommunityJoinRequestResponse>>.Failure(ResponseMessages.ProfileNotFound);

        var community = await _communityRepo.FirstOrDefaultAsync(c => c.Slug == slug);
        if (community is null)
            return Result<PagedResult<CommunityJoinRequestResponse>>.Failure(ResponseMessages.CommunityNotFound);

        var membership = await _memberRepo.FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.ProfileId == profile.Id);
        if (membership is null || (membership.Role != "owner" && membership.Role != "co_leader"))
            return Result<PagedResult<CommunityJoinRequestResponse>>.Failure(ResponseMessages.NoPermission);

        var skip = (page - 1) * pageSize;

        var requests = await _joinRequestRepo.GetPagedAsync(
            r => r.CommunityId == community.Id && r.Status == "pending",
            r => r.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _joinRequestRepo.CountAsync(r => r.CommunityId == community.Id && r.Status == "pending");

        if (requests.Count == 0)
        {
            return Result<PagedResult<CommunityJoinRequestResponse>>.Success(new PagedResult<CommunityJoinRequestResponse>
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
            });
        }

        var profileIds = requests.Select(r => r.ProfileId).ToList();
        var profiles = await _profileRepo.GetListAsync(p => profileIds.Contains(p.Id));
        var profileMap = profiles.ToDictionary(p => p.Id);

        var items = requests.Select(r =>
        {
            var p = profileMap.GetValueOrDefault(r.ProfileId);
            return new CommunityJoinRequestResponse(
                r.Id,
                r.ProfileId,
                p?.Username ?? "unknown",
                p?.DisplayName ?? "Unknown",
                p?.ProfilePictureUrl,
                r.Status,
                r.CreatedAt
            );
        }).ToList();

        return Result<PagedResult<CommunityJoinRequestResponse>>.Success(new PagedResult<CommunityJoinRequestResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    public async Task<Result> ApproveJoinRequestAsync(Guid authUserId, Guid requestId)
    {
        var profile = await _profileRepo.FirstOrDefaultAsync(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        var request = await _joinRequestRepo.GetByIdAsync(requestId);
        if (request is null || request.Status != "pending")
            return Result.Failure(ResponseMessages.JoinRequestNotFound);

        if (request.ProfileId == profile.Id)
            return Result.Failure(ResponseMessages.CannotApproveOwnRequest);

        var community = await _communityRepo.GetByIdAsync(request.CommunityId);
        if (community is null)
            return Result.Failure(ResponseMessages.CommunityNotFound);

        var membership = await _memberRepo.FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.ProfileId == profile.Id);
        if (membership is null || (membership.Role != "owner" && membership.Role != "co_leader"))
            return Result.Failure(ResponseMessages.NoPermission);

        request.Status = "approved";
        request.RespondedAt = DateTime.UtcNow;
        _joinRequestRepo.Update(request);

        var newMember = new CommunityMember
        {
            Id = Guid.NewGuid(),
            CommunityId = community.Id,
            ProfileId = request.ProfileId,
            Role = "member",
            JoinedAt = DateTime.UtcNow,
        };

        await _memberRepo.CreateAsync(newMember);

        community.MemberCount++;
        community.UpdatedAt = DateTime.UtcNow;
        _communityRepo.Update(community);
        await _communityRepo.SaveChangesAsync();

        return Result.Success(ResponseMessages.JoinRequestApproved);
    }

    public async Task<Result> RejectJoinRequestAsync(Guid authUserId, Guid requestId)
    {
        var profile = await _profileRepo.FirstOrDefaultAsync(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        var request = await _joinRequestRepo.GetByIdAsync(requestId);
        if (request is null || request.Status != "pending")
            return Result.Failure(ResponseMessages.JoinRequestNotFound);

        var community = await _communityRepo.GetByIdAsync(request.CommunityId);
        if (community is null)
            return Result.Failure(ResponseMessages.CommunityNotFound);

        var membership = await _memberRepo.FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.ProfileId == profile.Id);
        if (membership is null || (membership.Role != "owner" && membership.Role != "co_leader"))
            return Result.Failure(ResponseMessages.NoPermission);

        request.Status = "rejected";
        request.RespondedAt = DateTime.UtcNow;
        _joinRequestRepo.Update(request);
        await _joinRequestRepo.SaveChangesAsync();

        return Result.Success(ResponseMessages.JoinRequestRejected);
    }

    public async Task<Result> InviteMemberAsync(Guid authUserId, string slug, string targetUsername)
    {
        var profile = await _profileRepo.FirstOrDefaultAsync(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        var community = await _communityRepo.FirstOrDefaultAsync(c => c.Slug == slug);
        if (community is null)
            return Result.Failure(ResponseMessages.CommunityNotFound);

        var membership = await _memberRepo.FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.ProfileId == profile.Id);
        if (membership is null || (membership.Role != "owner" && membership.Role != "co_leader"))
            return Result.Failure(ResponseMessages.NoPermission);

        var targetProfile = await _profileRepo.FirstOrDefaultAsync(p => p.Username == targetUsername);
        if (targetProfile is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        if (targetProfile.Id == profile.Id)
            return Result.Failure(ResponseMessages.CannotInviteYourself);

        var existingMember = await _memberRepo.FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.ProfileId == targetProfile.Id);
        if (existingMember is not null)
            return Result.Failure(ResponseMessages.AlreadyMember);

        var existingInvitation = await _invitationRepo.FirstOrDefaultAsync(i =>
            i.CommunityId == community.Id && i.ProfileId == targetProfile.Id && i.Status == "pending");
        if (existingInvitation is not null)
            return Result.Failure(ResponseMessages.InvitationAlreadyPending);

        var invitation = new CommunityInvitation
        {
            Id = Guid.NewGuid(),
            CommunityId = community.Id,
            ProfileId = targetProfile.Id,
            InvitedByProfileId = profile.Id,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
        };

        await _invitationRepo.CreateAsync(invitation);
        return Result.Success(ResponseMessages.InvitationSent);
    }

    public async Task<Result<PagedResult<CommunityInvitationResponse>>> GetPendingInvitationsAsync(Guid profileId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;

        var invitations = await _invitationRepo.GetPagedAsync(
            i => i.ProfileId == profileId && i.Status == "pending",
            i => i.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _invitationRepo.CountAsync(i => i.ProfileId == profileId && i.Status == "pending");

        if (invitations.Count == 0)
        {
            return Result<PagedResult<CommunityInvitationResponse>>.Success(new PagedResult<CommunityInvitationResponse>
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
            });
        }

        var communityIds = invitations.Select(i => i.CommunityId).ToList();
        var communities = await _communityRepo.GetListAsync(c => communityIds.Contains(c.Id));
        var communityMap = communities.ToDictionary(c => c.Id);

        var inviterIds = invitations.Select(i => i.InvitedByProfileId).ToList();
        var inviters = await _profileRepo.GetListAsync(p => inviterIds.Contains(p.Id));
        var inviterMap = inviters.ToDictionary(p => p.Id);

        var items = invitations.Select(i =>
        {
            var c = communityMap.GetValueOrDefault(i.CommunityId);
            var inv = inviterMap.GetValueOrDefault(i.InvitedByProfileId);
            return new CommunityInvitationResponse(
                i.Id,
                i.CommunityId,
                c?.Name ?? "unknown",
                c?.Slug ?? "unknown",
                i.InvitedByProfileId,
                inv?.Username ?? "unknown",
                i.Status,
                i.CreatedAt
            );
        }).ToList();

        return Result<PagedResult<CommunityInvitationResponse>>.Success(new PagedResult<CommunityInvitationResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    public async Task<Result<PagedResult<CommunityInvitationResponse>>> GetCommunityInvitationsAsync(Guid authUserId, string slug, int page, int pageSize)
    {
        var profile = await _profileRepo.FirstOrDefaultAsync(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result<PagedResult<CommunityInvitationResponse>>.Failure(ResponseMessages.ProfileNotFound);

        var community = await _communityRepo.FirstOrDefaultAsync(c => c.Slug == slug);
        if (community is null)
            return Result<PagedResult<CommunityInvitationResponse>>.Failure(ResponseMessages.CommunityNotFound);

        var membership = await _memberRepo.FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.ProfileId == profile.Id);
        if (membership is null || (membership.Role != "owner" && membership.Role != "co_leader"))
            return Result<PagedResult<CommunityInvitationResponse>>.Failure(ResponseMessages.NoPermission);

        var skip = (page - 1) * pageSize;

        var invitations = await _invitationRepo.GetPagedAsync(
            i => i.CommunityId == community.Id && i.Status == "pending",
            i => i.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _invitationRepo.CountAsync(i => i.CommunityId == community.Id && i.Status == "pending");

        if (invitations.Count == 0)
        {
            return Result<PagedResult<CommunityInvitationResponse>>.Success(new PagedResult<CommunityInvitationResponse>
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
            });
        }

        var profileIds = invitations.Select(i => i.ProfileId).ToList();
        var profiles = await _profileRepo.GetListAsync(p => profileIds.Contains(p.Id));
        var profileMap = profiles.ToDictionary(p => p.Id);

        var inviterIds = invitations.Select(i => i.InvitedByProfileId).ToList();
        var inviters = await _profileRepo.GetListAsync(p => inviterIds.Contains(p.Id));
        var inviterMap = inviters.ToDictionary(p => p.Id);

        var items = invitations.Select(i =>
        {
            var inv = inviterMap.GetValueOrDefault(i.InvitedByProfileId);
            return new CommunityInvitationResponse(
                i.Id,
                i.CommunityId,
                community.Name,
                community.Slug,
                i.InvitedByProfileId,
                inv?.Username ?? "unknown",
                i.Status,
                i.CreatedAt
            );
        }).ToList();

        return Result<PagedResult<CommunityInvitationResponse>>.Success(new PagedResult<CommunityInvitationResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    public async Task<Result> AcceptInvitationAsync(Guid profileId, Guid invitationId)
    {
        var invitation = await _invitationRepo.GetByIdAsync(invitationId);
        if (invitation is null || invitation.Status != "pending")
            return Result.Failure(ResponseMessages.InvitationNotFound);

        if (invitation.ProfileId != profileId)
            return Result.Failure(ResponseMessages.NoPermission);

        var community = await _communityRepo.GetByIdAsync(invitation.CommunityId);
        if (community is null)
            return Result.Failure(ResponseMessages.CommunityNotFound);

        invitation.Status = "accepted";
        invitation.RespondedAt = DateTime.UtcNow;
        _invitationRepo.Update(invitation);

        var existingMember = await _memberRepo.FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.ProfileId == profileId);
        if (existingMember is null)
        {
            var member = new CommunityMember
            {
                Id = Guid.NewGuid(),
                CommunityId = community.Id,
                ProfileId = profileId,
                Role = "member",
                JoinedAt = DateTime.UtcNow,
            };

            await _memberRepo.CreateAsync(member);

            community.MemberCount++;
            community.UpdatedAt = DateTime.UtcNow;
            _communityRepo.Update(community);
        }

        await _communityRepo.SaveChangesAsync();
        return Result.Success(ResponseMessages.InvitationAccepted);
    }

    public async Task<Result> DeclineInvitationAsync(Guid profileId, Guid invitationId)
    {
        var invitation = await _invitationRepo.GetByIdAsync(invitationId);
        if (invitation is null || invitation.Status != "pending")
            return Result.Failure(ResponseMessages.InvitationNotFound);

        if (invitation.ProfileId != profileId)
            return Result.Failure(ResponseMessages.NoPermission);

        invitation.Status = "declined";
        invitation.RespondedAt = DateTime.UtcNow;
        _invitationRepo.Update(invitation);
        await _invitationRepo.SaveChangesAsync();

        return Result.Success(ResponseMessages.InvitationDeclined);
    }

    public async Task<Result<PagedResult<CommunityJoinRequestResponse>>> GetMyJoinRequestsAsync(Guid profileId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;

        var requests = await _joinRequestRepo.GetPagedAsync(
            r => r.ProfileId == profileId,
            r => r.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _joinRequestRepo.CountAsync(r => r.ProfileId == profileId);

        if (requests.Count == 0)
        {
            return Result<PagedResult<CommunityJoinRequestResponse>>.Success(new PagedResult<CommunityJoinRequestResponse>
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
            });
        }

        var items = requests.Select(r => new CommunityJoinRequestResponse(
            r.Id,
            r.ProfileId,
            "", "", null,
            r.Status,
            r.CreatedAt
        )).ToList();

        return Result<PagedResult<CommunityJoinRequestResponse>>.Success(new PagedResult<CommunityJoinRequestResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    public async Task<Result<PostResponse>> CreateCommunityPostAsync(Guid authUserId, string slug, string content, List<MediaUploadData>? mediaFiles)
    {
        var profile = await _profileRepo.FirstOrDefaultAsync(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result<PostResponse>.Failure(ResponseMessages.ProfileNotFound);

        var community = await _communityRepo.FirstOrDefaultAsync(c => c.Slug == slug);
        if (community is null)
            return Result<PostResponse>.Failure(ResponseMessages.CommunityNotFound);

        var membership = await _memberRepo.FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.ProfileId == profile.Id);
        if (membership is null)
            return Result<PostResponse>.Failure(ResponseMessages.NotMember);

        var post = new Post
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            CommunityId = community.Id,
            Content = content,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _postRepo.CreateAsync(post);

        var mediaList = new List<PostMedia>();
        if (mediaFiles is not null && mediaFiles.Count > 0)
        {
            for (int i = 0; i < mediaFiles.Count; i++)
            {
                var media = mediaFiles[i];
                var ext = Path.GetExtension(media.FileName);
                var fileName = $"{profile.Id}_{Guid.NewGuid()}{ext}";
                var uploadResult = await _cloudinaryService.UploadAsync(media.FileStream, fileName, CloudinaryFolder.PostMedia);

                if (uploadResult.IsSuccess && uploadResult.Data is not null)
                {
                    var data = uploadResult.Data;
                    var postMedia = new PostMedia
                    {
                        Id = Guid.NewGuid(),
                        PostId = post.Id,
                        Url = data.Url,
                        PublicId = data.PublicId,
                        MediaType = GetMediaType(media.FileName) ?? "image",
                        Order = i,
                        Width = data.Width,
                        Height = data.Height,
                        SizeBytes = data.SizeBytes,
                        Format = data.Format,
                        DurationSeconds = data.DurationSeconds,
                        CreatedAt = DateTime.UtcNow,
                    };
                    mediaList.Add(postMedia);
                    await _mediaRepo.AddEntityAsync(postMedia);
                }
            }

            await _mediaRepo.SaveChangesAsync();
        }

        var author = new PostAuthorResponse(profile.Id, profile.Username, profile.DisplayName, profile.ProfilePictureUrl, false);
        return Result<PostResponse>.Success(BuildPostResponse(post, author, false, false, mediaList));
    }

    public async Task<Result<PagedResult<PostResponse>>> GetCommunityPostsAsync(string slug, Guid? currentProfileId, int page, int pageSize)
    {
        var community = await _communityRepo.FirstOrDefaultAsync(c => c.Slug == slug);
        if (community is null)
            return Result<PagedResult<PostResponse>>.Failure(ResponseMessages.CommunityNotFound);

        if (community.IsPrivate && currentProfileId.HasValue)
        {
            var isMember = await _memberRepo.FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.ProfileId == currentProfileId.Value);
            if (isMember is null)
                return Result<PagedResult<PostResponse>>.Failure(ResponseMessages.CannotJoinPrivate);
        }
        else if (community.IsPrivate && !currentProfileId.HasValue)
        {
            return Result<PagedResult<PostResponse>>.Failure(ResponseMessages.PrivateCommunityRequiresAuth);
        }

        var skip = (page - 1) * pageSize;

        var posts = await _postRepo.GetPagedAsync(
            p => p.CommunityId == community.Id,
            p => p.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _postRepo.CountAsync(p => p.CommunityId == community.Id);

        if (posts.Count == 0)
        {
            return Result<PagedResult<PostResponse>>.Success(new PagedResult<PostResponse>
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
            });
        }

        var profileIds = posts.Select(p => p.ProfileId).Distinct().ToList();
        var profiles = await _profileRepo.GetListAsync(p => profileIds.Contains(p.Id));
        var profileMap = profiles.ToDictionary(p => p.Id);

        Dictionary<Guid, List<PostMedia>> mediaMap = [];
        var postIds = posts.Select(p => p.Id).ToList();
        var allMedia = await _mediaRepo.GetListAsync(m => postIds.Contains(m.PostId));
        mediaMap = allMedia.GroupBy(m => m.PostId).ToDictionary(g => g.Key, g => g.ToList());

        HashSet<Guid> likedPostIds = [];
        HashSet<Guid> savedPostIds = [];
        if (currentProfileId.HasValue)
        {
            var likes = await _likeRepo.GetListAsync(l =>
                l.ProfileId == currentProfileId.Value && postIds.Contains(l.PostId));
            likedPostIds = likes.Select(l => l.PostId).ToHashSet();

            var saved = await _savedPostRepo.GetListAsync(s =>
                s.ProfileId == currentProfileId.Value && postIds.Contains(s.PostId));
            savedPostIds = saved.Select(s => s.PostId).ToHashSet();
        }

        var items = posts.Select(p =>
        {
            var prof = profileMap.GetValueOrDefault(p.ProfileId);
            var author = prof is not null
                ? new PostAuthorResponse(prof.Id, prof.Username, prof.DisplayName, prof.ProfilePictureUrl, false)
                : new PostAuthorResponse(p.ProfileId, "Unknown", "Unknown", null, false);
            var media = mediaMap.GetValueOrDefault(p.Id) ?? [];
            return BuildPostResponse(p, author, likedPostIds.Contains(p.Id), savedPostIds.Contains(p.Id), media);
        }).ToList();

        return Result<PagedResult<PostResponse>>.Success(new PagedResult<PostResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    private static PostResponse BuildPostResponse(Post post, PostAuthorResponse author, bool isLiked, bool isSaved, List<PostMedia> media)
    {
        return new PostResponse(
            post.Id,
            author,
            post.Content,
            media.OrderBy(m => m.Order).Select(m => new PostMediaResponse(
                m.Url,
                m.MediaType,
                m.Order,
                m.Width,
                m.Height,
                m.SizeBytes,
                m.Format,
                m.DurationSeconds
            )).ToList(),
            post.LikeCount,
            post.CommentCount,
            post.SaveCount,
            isLiked,
            isSaved,
            post.CreatedAt,
            post.UpdatedAt,
            post.IsRepost,
            post.IsThread,
            post.OriginalPostId,
            null
        );
    }

    private static string? GetMediaType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" => "image",
            ".mp4" or ".mov" or ".avi" or ".webm" => "video",
            _ => null
        };
    }

    private static CommunityResponse BuildCommunityResponse(Community community, Profile owner, bool isMember, string? memberRole, bool? hasPendingJoinRequest = null, bool? hasPendingInvitation = null)
    {
        var ownerResponse = BuildOwnerResponse(owner);

        return new CommunityResponse(
            community.Id,
            community.Name,
            community.Slug,
            community.Description,
            community.MemberCount,
            community.IsPrivate,
            community.BannerUrl,
            community.IconUrl,
            ownerResponse,
            isMember,
            memberRole,
            hasPendingJoinRequest,
            hasPendingInvitation,
            community.CreatedAt
        );
    }

    private static CommunityOwnerResponse BuildOwnerResponse(Profile owner)
    {
        return new CommunityOwnerResponse(
            owner.Id,
            owner.Username,
            owner.DisplayName,
            owner.ProfilePictureUrl
        );
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');
        if (slug.Length > 100)
            slug = slug[..100];
        if (string.IsNullOrEmpty(slug))
            slug = "community";
        return slug;
    }
}
