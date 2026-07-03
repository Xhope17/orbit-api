namespace Orbit.Application.Models.DTOs;

public record ProfileResponse(
    Guid Id,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    string? BannerUrl,
    string? Bio,
    int FollowersCount,
    int FollowingCount,
    bool IsVerified,
    UserPrefixResponse? Prefix,
    bool IsFollowing
);
