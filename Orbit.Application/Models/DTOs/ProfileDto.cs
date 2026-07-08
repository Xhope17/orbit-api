namespace Orbit.Application.Models.DTOs;

public record ProfileDto(
    Guid Id,
    string Username,
    string DisplayName,
    string? AvatarUrl,
    string? BannerUrl,
    string? Bio,
    int FollowersCount,
    int FollowingCount,
    bool IsVerified,
    UserPrefixDto? Prefix,
    bool IsFollowing
);
