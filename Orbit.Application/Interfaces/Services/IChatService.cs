using Orbit.Application.Common;
using Orbit.Application.Models.Responses;
using Orbit.Application.Models.DTOs;

namespace Orbit.Application.Interfaces.Services;

public interface IChatService
{
    Task<GenericResponse<ChatDto>> CreateConversationAsync(Guid currentProfileId, string targetUsername);
    Task<GenericResponse<List<ChatDto>>> GetConversationsAsync(Guid currentProfileId);
    Task<GenericResponse<PagedResult<MessageResponse>>> GetMessagesAsync(Guid currentProfileId, Guid conversationId, int page, int pageSize);
    Task<GenericResponse<MessageResponse>> SendMessageAsync(Guid currentProfileId, Guid conversationId, string content);
    Task<GenericResponse<string>> DeleteMessageAsync(Guid currentProfileId, Guid conversationId, Guid messageId);
    Task<ChatProfileInfo?> GetProfileInfoAsync(Guid profileId);
}
