namespace Orbit.Application.Models.DTOs;

public record CommunityJoinRequestResponse(
    Guid Id,
    Guid ProfileId,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    string Status,
    DateTime CreatedAt
);

public record CommunityInvitationResponse(
    Guid Id,
    Guid CommunityId,
    string CommunityName,
    string CommunitySlug,
    Guid InvitedByProfileId,
    string InvitedByUsername,
    string Status,
    DateTime CreatedAt
);
