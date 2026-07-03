namespace Orbit.Application.Services;

public record NotificationEvent(
    Guid TargetProfileId,
    string Type,
    Guid ActorProfileId,
    Guid PostId,
    Guid? CommentId,
    string? PostPreview,
    string? CommentPreview
);
