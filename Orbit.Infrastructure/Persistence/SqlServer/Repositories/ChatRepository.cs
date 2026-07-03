using Microsoft.EntityFrameworkCore;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Repositories;
using Orbit.Domain.Entities;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.SqlServer.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly OrbitDbContext _dbContext;

    public ChatRepository(OrbitDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ConversationWithDetails?> GetConversationDetailsAsync(Guid conversationId, Guid profileId)
    {
        var raw = await RawQuery(profileId)
            .FirstOrDefaultAsync(r => r.Id == conversationId);

        if (raw is null) return null;

        return MapToDetails(raw, profileId);
    }

    public async Task<Conversation?> GetExistingDmAsync(Guid profileId1, Guid profileId2)
    {
        return await _dbContext.Conversations
            .Where(c => c.ConversationType == "dm")
            .Where(c => c.Participants.Any(p => p.ProfileId == profileId1))
            .Where(c => c.Participants.Any(p => p.ProfileId == profileId2))
            .FirstOrDefaultAsync();
    }

    public async Task<List<ConversationWithDetails>> GetConversationsAsync(Guid profileId)
    {
        var rawList = await RawQuery(profileId)
            .OrderByDescending(r => r.LastMsgCreatedAt ?? r.CreatedAt)
            .ToListAsync();

        return rawList
            .Select(r => MapToDetails(r, profileId))
            .ToList();
    }

    public async Task<List<Message>> GetMessagesAsync(Guid conversationId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;

        return await _dbContext.Messages
            .Where(m => m.ConversationId == conversationId)
.OrderBy(m => m.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetMessagesCountAsync(Guid conversationId)
    {
        return await _dbContext.Messages
            .Where(m => m.ConversationId == conversationId)
            .CountAsync();
    }

    public async Task<bool> IsParticipantAsync(Guid conversationId, Guid profileId)
    {
        return await _dbContext.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.ProfileId == profileId);
    }

    public async Task<Message?> GetMessageOwnershipAsync(Guid messageId, Guid senderProfileId)
    {
        return await _dbContext.Messages
            .Where(m => m.Id == messageId && m.SenderProfileId == senderProfileId)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> HasMutualFollowAsync(Guid profileId1, Guid profileId2)
    {
        var count = await _dbContext.Follows
            .CountAsync(f =>
                (f.FollowerId == profileId1 && f.FollowingId == profileId2) ||
                (f.FollowerId == profileId2 && f.FollowingId == profileId1));

        return count == 2;
    }

    public async Task CreateConversationAsync(Conversation conversation, List<ConversationParticipant> participants)
    {
        _dbContext.Conversations.Add(conversation);
        _dbContext.ConversationParticipants.AddRange(participants);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddMessageAsync(Message message)
    {
        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }

    private IQueryable<ConversationRaw> RawQuery(Guid profileId)
    {
        return _dbContext.Conversations
            .Where(c => c.Participants.Any(p => p.ProfileId == profileId))
            .Select(c => new ConversationRaw
            {
                Id = c.Id,
                ConversationType = c.ConversationType,
                CreatedAt = c.CreatedAt,
                OtherId = c.Participants.Where(p => p.ProfileId != profileId).Select(p => p.Profile.Id).FirstOrDefault(),
                OtherUsername = c.Participants.Where(p => p.ProfileId != profileId).Select(p => p.Profile.Username).FirstOrDefault() ?? "",
                OtherDisplayName = c.Participants.Where(p => p.ProfileId != profileId).Select(p => p.Profile.DisplayName).FirstOrDefault() ?? "",
                OtherAvatarUrl = c.Participants.Where(p => p.ProfileId != profileId).Select(p => p.Profile.ProfilePictureUrl).FirstOrDefault(),
                LastMsgId = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => (Guid?)m.Id).FirstOrDefault(),
                LastMsgConvId = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => (Guid?)m.ConversationId).FirstOrDefault(),
                LastMsgSenderId = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => (Guid?)m.SenderProfileId).FirstOrDefault(),
                LastMsgContent = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Content).FirstOrDefault(),
                LastMsgIsSeen = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => (bool?)m.IsSeen).FirstOrDefault(),
                LastMsgIsEdited = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => (bool?)m.IsEdited).FirstOrDefault(),
                LastMsgEditedAt = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => (DateTime?)m.EditedAt).FirstOrDefault(),
                LastMsgCreatedAt = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => (DateTime?)m.CreatedAt).FirstOrDefault(),
                LastMsgDeletedAt = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => (DateTime?)m.DeletedAt).FirstOrDefault(),
                UnreadCount = c.Messages.Count(m => !m.IsSeen && m.SenderProfileId != profileId)
            });
    }

    private static ConversationWithDetails MapToDetails(ConversationRaw raw, Guid profileId)
    {
        return new ConversationWithDetails
        {
            ConversationId = raw.Id,
            ConversationType = raw.ConversationType,
            CreatedAt = raw.CreatedAt,
            OtherParticipant = new ChatProfileInfo(
                raw.OtherId,
                raw.OtherUsername,
                raw.OtherDisplayName,
                raw.OtherAvatarUrl
            ),
            LastMessage = raw.LastMsgId is not null
                ? new MessageResponse(
                    raw.LastMsgId ?? Guid.Empty,
                    raw.LastMsgConvId ?? Guid.Empty,
                    raw.LastMsgSenderId ?? Guid.Empty,
                    raw.LastMsgContent,
                    raw.LastMsgIsSeen ?? false,
                    raw.LastMsgIsEdited ?? false,
                    raw.LastMsgEditedAt,
                    raw.LastMsgCreatedAt ?? DateTime.MinValue,
                    raw.LastMsgDeletedAt,
                    raw.LastMsgSenderId == profileId
                  )
                : null,
            UnreadCount = raw.UnreadCount,
            IsLastMessageFromCurrentUser = raw.LastMsgSenderId == profileId
        };
    }

    private class ConversationRaw
    {
        public Guid Id { get; set; }
        public string ConversationType { get; set; } = "dm";
        public DateTime CreatedAt { get; set; }
        public Guid OtherId { get; set; }
        public string OtherUsername { get; set; } = "";
        public string OtherDisplayName { get; set; } = "";
        public string? OtherAvatarUrl { get; set; }
        public Guid? LastMsgId { get; set; }
        public Guid? LastMsgConvId { get; set; }
        public Guid? LastMsgSenderId { get; set; }
        public string? LastMsgContent { get; set; }
        public bool? LastMsgIsSeen { get; set; }
        public bool? LastMsgIsEdited { get; set; }
        public DateTime? LastMsgEditedAt { get; set; }
        public DateTime? LastMsgCreatedAt { get; set; }
        public DateTime? LastMsgDeletedAt { get; set; }
        public int UnreadCount { get; set; }
    }
}
