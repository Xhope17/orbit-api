using Orbit.Application.Models.DTOs;
using Orbit.Domain.Entities;

namespace Orbit.Application.Interfaces.Repositories;

public class ConversationWithDetails
{
    public Guid ConversationId { get; init; }
    public string ConversationType { get; init; } = "dm";
    public DateTime CreatedAt { get; init; }
    public ChatProfileInfo OtherParticipant { get; init; } = null!;
    public MessageResponse? LastMessage { get; init; }
    public int UnreadCount { get; init; }
    public bool IsLastMessageFromCurrentUser { get; init; }
}

public interface IChatRepository
{
    Task<ConversationWithDetails?> GetConversationDetailsAsync(Guid conversationId, Guid profileId);
    Task<Conversation?> GetExistingDmAsync(Guid profileId1, Guid profileId2);
    Task<List<ConversationWithDetails>> GetConversationsAsync(Guid profileId);
    Task<List<Message>> GetMessagesAsync(Guid conversationId, int page, int pageSize);
    Task<int> GetMessagesCountAsync(Guid conversationId);
    Task<bool> IsParticipantAsync(Guid conversationId, Guid profileId);
    Task<Message?> GetMessageOwnershipAsync(Guid messageId, Guid senderProfileId);
    Task<bool> HasMutualFollowAsync(Guid profileId1, Guid profileId2);
    Task CreateConversationAsync(Conversation conversation, List<ConversationParticipant> participants);
    Task AddMessageAsync(Message message);
    Task SaveChangesAsync();
}
