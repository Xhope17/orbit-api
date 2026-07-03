namespace Orbit.Application.Models.DTOs;

public record NotificationResponse(
    Guid Id,
    string Type,
    PostAuthorResponse Actor,
    Guid? PostId,
    string? PostPreview,
    Guid? CommentId,
    string? CommentPreview,
    int TotalCount,
    bool IsRead,
    DateTime CreatedAt
);
