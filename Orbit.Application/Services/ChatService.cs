using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Helpers;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Models.Responses;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.Entities;
using Orbit.Application.Interfaces.Repositories;
using Orbit.Domain.DataBase;
using Orbit.Domain.Exceptions;
using Orbit.Shared.Constants;

namespace Orbit.Application.Services;

public class ChatService : IChatService
{
    private readonly IChatRepository _chatRepo;
    private readonly IUnitOfWork _uow;

    public ChatService(IChatRepository chatRepo, IUnitOfWork uow)
    {
        _chatRepo = chatRepo;
        _uow = uow;
    }

    public async Task<GenericResponse<ChatDto>> CreateConversationAsync(Guid currentProfileId, string targetUsername)
    {
        var slug = targetUsername.ToLowerInvariant();
        var targetProfile = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (targetProfile is null)
            throw new NotFoundException(ResponseMessages.ProfileNotFound);

        if (targetProfile.Id == currentProfileId)
            throw new BadRequestException(ResponseMessages.CannotChatYourself);

        if (targetProfile.IsPrivate)
        {
            var hasMutual = await _chatRepo.HasMutualFollowAsync(currentProfileId, targetProfile.Id);
            if (!hasMutual)
                throw new BadRequestException(ResponseMessages.MutualFollowRequired);
        }

        var existing = await _chatRepo.GetExistingDmAsync(currentProfileId, targetProfile.Id);
        if (existing is not null)
        {
            var details = await _chatRepo.GetConversationDetailsAsync(existing.Id, currentProfileId);
            if (details is not null)
                return ResponseHelper.Create(MapToResponse(details));
        }

        return ResponseHelper.Create(new ChatDto(
            Guid.Empty,
            new ChatProfileInfo(targetProfile.Id, targetProfile.Username, targetProfile.DisplayName, targetProfile.ProfilePictureUrl),
            null, 0, DateTime.UtcNow, false, true
        ));
    }

    public async Task<GenericResponse<List<ChatDto>>> GetConversationsAsync(Guid currentProfileId)
    {
        var conversations = await _chatRepo.GetConversationsAsync(currentProfileId);

        return ResponseHelper.Create(
            conversations.Select(MapToResponse).ToList()
        );
    }

    public async Task<GenericResponse<PagedResult<MessageResponse>>> GetMessagesAsync(
        Guid currentProfileId, Guid conversationId, int page, int pageSize)
    {
        var isParticipant = await _chatRepo.IsParticipantAsync(conversationId, currentProfileId);
        if (!isParticipant)
            throw new BadRequestException(ResponseMessages.NotConversationParticipant);

        var messages = await _chatRepo.GetMessagesAsync(conversationId, page, pageSize);
        var totalCount = await _chatRepo.GetMessagesCountAsync(conversationId);

        return ResponseHelper.Create(new PagedResult<MessageResponse>
        {
            Items = messages.Select(m => new MessageResponse(
                m.Id, m.ConversationId, m.SenderProfileId, m.Content,
                m.IsSeen, m.IsEdited, m.EditedAt, m.CreatedAt, m.DeletedAt,
                m.SenderProfileId == currentProfileId
            )).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    public async Task<GenericResponse<MessageResponse>> SendMessageAsync(
        Guid currentProfileId, Guid conversationId, string content)
    {
        var isParticipant = await _chatRepo.IsParticipantAsync(conversationId, currentProfileId);
        if (!isParticipant)
            throw new BadRequestException(ResponseMessages.NotConversationParticipant);

        if (string.IsNullOrWhiteSpace(content))
            throw new BadRequestException(ResponseMessages.MessageContentRequired);

        if (content.Length > DomainConstants.MessageContentMaxLength)
            throw new BadRequestException(ResponseMessages.MessageContentMaxLength);

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderProfileId = currentProfileId,
            Content = content,
            IsSeen = false,
            IsEdited = false,
            CreatedAt = DateTime.UtcNow,
        };

        await _chatRepo.AddMessageAsync(message);

        return ResponseHelper.Create(new MessageResponse(
            message.Id,
            message.ConversationId,
            message.SenderProfileId,
            message.Content,
            message.IsSeen,
            message.IsEdited,
            message.EditedAt,
            message.CreatedAt,
            message.DeletedAt,
            true
        ));
    }

    public async Task<GenericResponse<string>> DeleteMessageAsync(
        Guid currentProfileId, Guid conversationId, Guid messageId)
    {
        var isParticipant = await _chatRepo.IsParticipantAsync(conversationId, currentProfileId);
        if (!isParticipant)
            throw new BadRequestException(ResponseMessages.NotConversationParticipant);

        var message = await _chatRepo.GetMessageOwnershipAsync(messageId, currentProfileId);
        if (message is null)
            throw new NotFoundException(ResponseMessages.MessageNotFound);

        if (message.ConversationId != conversationId)
            throw new NotFoundException(ResponseMessages.MessageNotFound);

        message.DeletedAt = DateTime.UtcNow;
        await _chatRepo.SaveChangesAsync();

        return ResponseHelper.Create<string>(default, message: ResponseMessages.MessageDeleted);
    }

    public async Task<ChatProfileInfo?> GetProfileInfoAsync(Guid profileId)
    {
        var profile = await _uow.profileRepository.Get(p => p.Id == profileId);
        if (profile is null) return null;
        return new ChatProfileInfo(profile.Id, profile.Username, profile.DisplayName, profile.ProfilePictureUrl);
    }

    private static ChatDto MapToResponse(ConversationWithDetails details)
    {
        return new ChatDto(
            details.ConversationId,
            details.OtherParticipant,
            details.LastMessage,
            details.UnreadCount,
            details.CreatedAt,
            details.IsLastMessageFromCurrentUser
        );
    }
}
