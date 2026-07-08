namespace Orbit.Application.Models.DTOs;

public record NotificationDto(
    Guid Id,
    string Type,
    PostAuthorDto Actor,
    Guid? PostId,
    string? PostPreview,
    Guid? CommentId,
    string? CommentPreview,
    int TotalCount,
    bool IsRead,
    DateTime CreatedAt
);
