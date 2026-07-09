using Orbit.Application.Common;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Models.Responses;

namespace Orbit.Application.Interfaces.Services;

public interface ICommunityService
{
    Task<GenericResponse<CommunityDto>> CreateCommunityAsync(Guid authUserId, string name, string? description, bool isPrivate);
    Task<GenericResponse<CommunityDto>> UpdateCommunityAsync(Guid authUserId, string slug, string? name, string? description, bool? isPrivate);
    Task<GenericResponse<string>> DeleteCommunityAsync(Guid authUserId, string slug);
    Task<GenericResponse<CommunityDto>> GetCommunityAsync(string slug, Guid? currentProfileId);
    Task<GenericResponse<PagedResult<CommunitySummaryDto>>> SearchCommunitiesAsync(string? query, int page, int pageSize, Guid? currentProfileId = null);
    Task<GenericResponse<PagedResult<CommunitySummaryDto>>> GetMyCommunitiesAsync(Guid profileId, int page, int pageSize);
    Task<GenericResponse<string>> JoinCommunityAsync(Guid profileId, string slug);
    Task<GenericResponse<string>> LeaveCommunityAsync(Guid profileId, string slug);
    Task<GenericResponse<string>> KickMemberAsync(Guid authUserId, string slug, Guid targetProfileId);
    Task<GenericResponse<string>> AssignCoLeaderAsync(Guid authUserId, string slug, string targetUsername);
    Task<GenericResponse<string>> RemoveCoLeaderAsync(Guid authUserId, string slug, string targetUsername);
    Task<GenericResponse<PagedResult<CommunityMemberResponse>>> GetMembersAsync(string slug, int page, int pageSize, Guid? currentProfileId);
    Task<GenericResponse<CommunityJoinRequestResponse>> RequestJoinAsync(Guid profileId, string slug);
    Task<GenericResponse<PagedResult<CommunityJoinRequestResponse>>> GetJoinRequestsAsync(Guid authUserId, string slug, int page, int pageSize);
    Task<GenericResponse<string>> ApproveJoinRequestAsync(Guid authUserId, Guid requestId);
    Task<GenericResponse<string>> RejectJoinRequestAsync(Guid authUserId, Guid requestId);
    Task<GenericResponse<string>> InviteMemberAsync(Guid authUserId, string slug, string targetUsername);
    Task<GenericResponse<PagedResult<CommunityInvitationResponse>>> GetPendingInvitationsAsync(Guid profileId, int page, int pageSize);
    Task<GenericResponse<PagedResult<CommunityInvitationResponse>>> GetCommunityInvitationsAsync(Guid authUserId, string slug, int page, int pageSize);
    Task<GenericResponse<string>> AcceptInvitationAsync(Guid profileId, Guid invitationId);
    Task<GenericResponse<string>> DeclineInvitationAsync(Guid profileId, Guid invitationId);
    Task<GenericResponse<PagedResult<CommunityJoinRequestResponse>>> GetMyJoinRequestsAsync(Guid profileId, int page, int pageSize);
    Task<GenericResponse<PostDto>> CreateCommunityPostAsync(Guid authUserId, string slug, string content, List<MediaUploadData>? mediaFiles);
    Task<GenericResponse<PagedResult<PostDto>>> GetCommunityPostsAsync(string slug, Guid? currentProfileId, int page, int pageSize);
}
