namespace Orbit.Application.Models.Responses;

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
