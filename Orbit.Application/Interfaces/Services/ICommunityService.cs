using Orbit.Application.Common;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Models.Responses;

namespace Orbit.Application.Interfaces.Services;

public interface ICommunityService
{
    Task<Result<CommunityDto>> CreateCommunityAsync(Guid authUserId, string name, string? description, bool isPrivate);
    Task<Result<CommunityDto>> UpdateCommunityAsync(Guid authUserId, string slug, string? name, string? description, bool? isPrivate);
    Task<Result> DeleteCommunityAsync(Guid authUserId, string slug);
    Task<Result<CommunityDto>> GetCommunityAsync(string slug, Guid? currentProfileId);
    Task<Result<PagedResult<CommunitySummaryDto>>> SearchCommunitiesAsync(string? query, int page, int pageSize, Guid? currentProfileId = null);
    Task<Result<PagedResult<CommunitySummaryDto>>> GetMyCommunitiesAsync(Guid profileId, int page, int pageSize);
    Task<Result> JoinCommunityAsync(Guid profileId, string slug);
    Task<Result> LeaveCommunityAsync(Guid profileId, string slug);
    Task<Result> KickMemberAsync(Guid authUserId, string slug, Guid targetProfileId);
    Task<Result> AssignCoLeaderAsync(Guid authUserId, string slug, string targetUsername);
    Task<Result> RemoveCoLeaderAsync(Guid authUserId, string slug, string targetUsername);
    Task<Result<PagedResult<CommunityMemberResponse>>> GetMembersAsync(string slug, int page, int pageSize, Guid? currentProfileId);
    Task<Result<CommunityJoinRequestResponse>> RequestJoinAsync(Guid profileId, string slug);
    Task<Result<PagedResult<CommunityJoinRequestResponse>>> GetJoinRequestsAsync(Guid authUserId, string slug, int page, int pageSize);
    Task<Result> ApproveJoinRequestAsync(Guid authUserId, Guid requestId);
    Task<Result> RejectJoinRequestAsync(Guid authUserId, Guid requestId);
    Task<Result> InviteMemberAsync(Guid authUserId, string slug, string targetUsername);
    Task<Result<PagedResult<CommunityInvitationResponse>>> GetPendingInvitationsAsync(Guid profileId, int page, int pageSize);
    Task<Result<PagedResult<CommunityInvitationResponse>>> GetCommunityInvitationsAsync(Guid authUserId, string slug, int page, int pageSize);
    Task<Result> AcceptInvitationAsync(Guid profileId, Guid invitationId);
    Task<Result> DeclineInvitationAsync(Guid profileId, Guid invitationId);
    Task<Result<PagedResult<CommunityJoinRequestResponse>>> GetMyJoinRequestsAsync(Guid profileId, int page, int pageSize);
    Task<Result<PostDto>> CreateCommunityPostAsync(Guid authUserId, string slug, string content, List<MediaUploadData>? mediaFiles);
    Task<Result<PagedResult<PostDto>>> GetCommunityPostsAsync(string slug, Guid? currentProfileId, int page, int pageSize);
}
