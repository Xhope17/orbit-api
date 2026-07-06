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

    private static string ProfileGroup(Guid profileId) => $"profile:{profileId}";
    private static string ConversationGroup(Guid conversationId) => $"conversation:{conversationId}";

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

        var (wasCreated, targetProfile) = await ResolveConversationAsync(conversationId, profileId, targetProfileId);

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

        var messageDto = BuildMessageBroadcast(message, sender);

        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));

        if (wasCreated)
        {
            await NotifyNewConversationAsync(conversationId, profileId, targetProfile!, sender, message, messageDto);
        }

        var otherParticipantId = wasCreated
            ? targetProfileId!.Value
            : await _dbContext.ConversationParticipants
                .Where(cp => cp.ConversationId == conversationId && cp.ProfileId != profileId)
                .Select(cp => cp.ProfileId)
                .FirstOrDefaultAsync();

        if (otherParticipantId != default)
        {
            await Clients.Group(ProfileGroup(otherParticipantId))
                .SendAsync("ReceiveMessage", messageDto);
        }

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

    private async Task<(bool wasCreated, Profile? targetProfile)> ResolveConversationAsync(
        Guid conversationId, Guid profileId, Guid? targetProfileId)
    {
        if (conversationId != Guid.Empty)
        {
            var isParticipant = await _dbContext.ConversationParticipants
                .AnyAsync(cp => cp.ConversationId == conversationId && cp.ProfileId == profileId);

            if (!isParticipant)
                throw new HubException("You are not a participant of this conversation");

            return (false, null);
        }

        if (targetProfileId is null || targetProfileId.Value == Guid.Empty)
            throw new HubException("Target profile is required for a new conversation");

        if (targetProfileId.Value == profileId)
            throw new HubException(ResponseMessages.CannotChatYourself);

        var targetProfile = await _dbContext.Profiles.FindAsync(targetProfileId.Value);
        if (targetProfile is null)
            throw new HubException(ResponseMessages.ProfileNotFound);

        if (targetProfile.IsPrivate)
        {
            var mutualCount = await _dbContext.Follows
                .CountAsync(f =>
                    (f.FollowerId == profileId && f.FollowingId == targetProfileId) ||
                    (f.FollowerId == targetProfileId && f.FollowingId == profileId));
            if (mutualCount != 2)
                throw new HubException(ResponseMessages.MutualFollowRequired);
        }

        var existing = await _dbContext.Conversations
            .Where(c => c.ConversationType == "dm")
            .Where(c => c.Participants.Any(p => p.ProfileId == profileId))
            .Where(c => c.Participants.Any(p => p.ProfileId == targetProfileId))
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            return (false, targetProfile);
        }

        var newId = Guid.NewGuid();
        var conversation = new Conversation
        {
            Id = newId,
            ConversationType = "dm",
            CreatedAt = DateTime.UtcNow,
        };

        var participants = new List<ConversationParticipant>
        {
            new() { ConversationId = newId, ProfileId = profileId, JoinedAt = DateTime.UtcNow },
            new() { ConversationId = newId, ProfileId = targetProfileId.Value, JoinedAt = DateTime.UtcNow },
        };

        _dbContext.Conversations.Add(conversation);
        _dbContext.ConversationParticipants.AddRange(participants);

        return (true, targetProfile);
    }

    private static ChatMessageBroadcast BuildMessageBroadcast(Message message, dynamic sender)
    {
        return new ChatMessageBroadcast(
            message.Id, message.ConversationId, message.Content,
            message.IsSeen, message.IsEdited, message.EditedAt,
            message.CreatedAt, message.DeletedAt,
            new ChatProfileInfo(sender.Id, sender.Username, sender.DisplayName, sender.ProfilePictureUrl)
        );
    }

    private async Task NotifyNewConversationAsync(
        Guid conversationId, Guid profileId, Profile targetProfile,
        dynamic sender, Message message, ChatMessageBroadcast messageDto)
    {
        var lastMsgForOther = new MessageResponse(
            message.Id, conversationId, profileId, message.Content,
            message.IsSeen, message.IsEdited, message.EditedAt,
            message.CreatedAt, message.DeletedAt, false
        );

        var lastMsgForCaller = new MessageResponse(
            message.Id, conversationId, profileId, message.Content,
            message.IsSeen, message.IsEdited, message.EditedAt,
            message.CreatedAt, message.DeletedAt, true
        );

        var convForOther = new ChatResponse(
            conversationId,
            new ChatProfileInfo(sender.Id, sender.Username, sender.DisplayName, sender.ProfilePictureUrl),
            lastMsgForOther, 1, DateTime.UtcNow, false
        );

        var convForCaller = new ChatResponse(
            conversationId,
            new ChatProfileInfo(targetProfile.Id, targetProfile.Username, targetProfile.DisplayName, targetProfile.ProfilePictureUrl),
            lastMsgForCaller, 0, DateTime.UtcNow, false
        );

        await Clients.Group(ProfileGroup(targetProfile.Id))
            .SendAsync("NewConversation", convForOther);
        await Clients.Caller.SendAsync("NewConversation", convForCaller);
    }
}
