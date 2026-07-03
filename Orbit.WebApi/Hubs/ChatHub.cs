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

    public async Task SendMessage(Guid conversationId, string content)
    {
        var profileId = GetProfileId();

        var isParticipant = await _dbContext.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.ProfileId == profileId);

        if (!isParticipant)
            throw new HubException("You are not a participant of this conversation");

        if (string.IsNullOrWhiteSpace(content))
            throw new HubException(ResponseMessages.MessageContentRequired);

        if (content.Length > DomainConstants.MessageContentMaxLength)
            throw new HubException(ResponseMessages.MessageContentMaxLength);

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
