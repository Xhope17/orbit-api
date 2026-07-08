namespace Orbit.Application.Models.DTOs;

public record ChatProfileInfo(
    Guid ProfileId,
    string Username,
    string DisplayName,
    string? AvatarUrl
);

public record ChatMessageBroadcast(
    Guid Id,
    Guid ConversationId,
    string? Content,
    bool IsSeen,
    bool IsEdited,
    DateTime? EditedAt,
    DateTime CreatedAt,
    DateTime? DeletedAt,
    ChatProfileInfo Sender
);

public record MessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderProfileId,
    string? Content,
    bool IsSeen,
    bool IsEdited,
    DateTime? EditedAt,
    DateTime CreatedAt,
    DateTime? DeletedAt,
    bool IsFromCurrentUser
);

public record ChatDto(
    Guid Id,
    ChatProfileInfo OtherParticipant,
    MessageResponse? LastMessage,
    int UnreadCount,
    DateTime CreatedAt,
    bool IsLastMessageFromCurrentUser,
    bool IsPlaceholder = false
);
