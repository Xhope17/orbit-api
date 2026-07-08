using Orbit.Application.Models.DTOs;

namespace Orbit.Application.Models.Responses;

public record CommunityJoinRequestResponse(
    Guid Id,
    Guid ProfileId,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    string Status,
    DateTime CreatedAt
);
