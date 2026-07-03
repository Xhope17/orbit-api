namespace Orbit.Application.Models.DTOs;

public record CommunityOwnerResponse(
    Guid ProfileId,
    string Username,
    string DisplayName,
    string? AvatarUrl
);

public record CommunityMemberResponse(
    Guid ProfileId,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    string Role,
    DateTime JoinedAt
);

public record CommunityResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    int MemberCount,
    bool IsPrivate,
    string? BannerUrl,
    string? IconUrl,
    CommunityOwnerResponse Owner,
    bool IsMember,
    string? MemberRole,
    bool? HasPendingJoinRequest,
    bool? HasPendingInvitation,
    DateTime CreatedAt
);

public record CommunitySummaryResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    int MemberCount,
    bool IsPrivate,
    string? IconUrl,
    bool? HasPendingJoinRequest,
    bool? HasPendingInvitation
);
