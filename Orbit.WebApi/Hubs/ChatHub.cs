using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Orbit.Application.Constants;
using Orbit.Application.Models.DTOs;
using Orbit.Domain.Entities;
using Orbit.Domain.DataBase.Context;
using Orbit.Shared.Constants;

namespace Orbit.WebApi.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly OrbitDbContext _dbContext;

    public ChatHub(OrbitDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private Guid GetProfileId()
    {
        var claim = Context.User?.FindFirst(ClaimConstants.ProfileId)?.Value;
        if (claim is null || !Guid.TryParse(claim, out var profileId))
            throw new HubException("User not authenticated");
        return profileId;
    }

    private string ProfileGroup(Guid profileId) => $"profile:{profileId}";
    private string ConversationGroup(Guid conversationId) => $"conversation:{conversationId}";

    public override async Task OnConnectedAsync()
    {
        var profileId = GetProfileId();
        await Groups.AddToGroupAsync(Context.ConnectionId, ProfileGroup(profileId));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var profileId = GetProfileId();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ProfileGroup(profileId));
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var profileId = GetProfileId();
        var isParticipant = await _dbContext.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.ProfileId == profileId);

        if (!isParticipant)
            throw new HubException("You are not a participant of this conversation");

        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
    }

    public async Task SendMessage(Guid conversationId, string content, Guid? targetProfileId = null)
    {
        var profileId = GetProfileId();

        if (string.IsNullOrWhiteSpace(content))
            throw new HubException(ResponseMessages.MessageContentRequired);

        if (content.Length > DomainConstants.MessageContentMaxLength)
            throw new HubException(ResponseMessages.MessageContentMaxLength);

        if (conversationId == Guid.Empty)
        {
            if (targetProfileId is null || targetProfileId.Value == Guid.Empty)
                throw new HubException("Target profile is required for a new conversation");

            if (targetProfileId.Value == profileId)
                throw new HubException(ResponseMessages.CannotChatYourself);

            var targetProfile = await _dbContext.Profiles.FindAsync(targetProfileId.Value);
            if (targetProfile is null)
                throw new HubException(ResponseMessages.ProfileNotFound);

            if (targetProfile.IsPrivate)
            {
                var hasMutual = await _dbContext.Follows
                    .CountAsync(f =>
                        (f.FollowerId == profileId && f.FollowingId == targetProfileId) ||
                        (f.FollowerId == targetProfileId && f.FollowingId == profileId)) == 2;
                if (!hasMutual)
                    throw new HubException(ResponseMessages.MutualFollowRequired);
            }

            var existing = await _dbContext.Conversations
                .Where(c => c.ConversationType == "dm")
                .Where(c => c.Participants.Any(p => p.ProfileId == profileId))
                .Where(c => c.Participants.Any(p => p.ProfileId == targetProfileId))
                .FirstOrDefaultAsync();

            if (existing is not null)
                conversationId = existing.Id;
            else
            {
                conversationId = Guid.NewGuid();
                var conversation = new Conversation
                {
                    Id = conversationId,
                    ConversationType = "dm",
                    CreatedAt = DateTime.UtcNow,
                };

                var participants = new List<ConversationParticipant>
                {
                    new() { ConversationId = conversationId, ProfileId = profileId, JoinedAt = DateTime.UtcNow },
                    new() { ConversationId = conversationId, ProfileId = targetProfileId.Value, JoinedAt = DateTime.UtcNow },
                };

                _dbContext.Conversations.Add(conversation);
                _dbContext.ConversationParticipants.AddRange(participants);
            }
        }
        else
        {
            var isParticipant = await _dbContext.ConversationParticipants
                .AnyAsync(cp => cp.ConversationId == conversationId && cp.ProfileId == profileId);

            if (!isParticipant)
                throw new HubException("You are not a participant of this conversation");
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderProfileId = profileId,
            Content = content,
            IsSeen = false,
            IsEdited = false,
            CreatedAt = DateTime.UtcNow,
        };

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();

        var sender = await _dbContext.Profiles
            .Where(p => p.Id == profileId)
            .Select(p => new { p.Id, p.Username, p.DisplayName, p.ProfilePictureUrl })
            .FirstAsync();

        var messageDto = new ChatMessageBroadcast(
            message.Id,
            message.ConversationId,
            message.Content,
            message.IsSeen,
            message.IsEdited,
            message.EditedAt,
            message.CreatedAt,
            message.DeletedAt,
            new ChatProfileInfo(sender.Id, sender.Username, sender.DisplayName, sender.ProfilePictureUrl)
        );

        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));

        await Clients.OthersInGroup(ConversationGroup(conversationId))
            .SendAsync("ReceiveMessage", messageDto);

        await Clients.Caller.SendAsync("ReceiveOwnMessage", messageDto);
    }

    public async Task MarkAsRead(Guid conversationId)
    {
        var profileId = GetProfileId();

        var updated = await _dbContext.Messages
            .Where(m => m.ConversationId == conversationId
                     && m.SenderProfileId != profileId
                     && !m.IsSeen
                     && m.DeletedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.IsSeen, true));

        if (updated > 0)
        {
            await Clients.OthersInGroup(ConversationGroup(conversationId))
                .SendAsync("MessageRead", new { conversationId, readByProfileId = profileId });
        }
    }

    public async Task Typing(Guid conversationId)
    {
        var profileId = GetProfileId();

        await Clients.OthersInGroup(ConversationGroup(conversationId))
            .SendAsync("UserTyping", new { conversationId, profileId });
    }
}
