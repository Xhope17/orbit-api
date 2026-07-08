using Orbit.Application.Common;
using Orbit.Application.Models.DTOs;

namespace Orbit.Application.Interfaces.Services;

public interface IChatService
{
    Task<Result<ChatDto>> CreateConversationAsync(Guid currentProfileId, string targetUsername);
    Task<Result<List<ChatDto>>> GetConversationsAsync(Guid currentProfileId);
    Task<Result<PagedResult<MessageResponse>>> GetMessagesAsync(Guid currentProfileId, Guid conversationId, int page, int pageSize);
    Task<Result<MessageResponse>> SendMessageAsync(Guid currentProfileId, Guid conversationId, string content);
    Task<Result> DeleteMessageAsync(Guid currentProfileId, Guid conversationId, Guid messageId);
    Task<ChatProfileInfo?> GetProfileInfoAsync(Guid profileId);
}
