using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Orbit.WebApi.Models;
using Orbit.WebApi.Hubs;
using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Models.Responses;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.Exceptions;
using Orbit.WebApi.Helpers;

namespace Orbit.WebApi.Controllers;

[Authorize]
public class ChatController : BaseController
{
    private readonly IChatService _chatService;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatController(
        IChatService chatService,
        IHubContext<ChatHub> hubContext)
    {
        _chatService = chatService;
        _hubContext = hubContext;
    }

    [HttpPost("api/chats")]
    [EndpointSummary("Crear conversación")]
    [EndpointDescription("Crea una nueva conversación con otro usuario.")]
    [ProducesResponseType<GenericResponse<ChatDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<ChatDto>> CreateConversation([FromBody] CreateChatRequest request)
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _chatService.CreateConversationAsync(profileId, request.Username);

        if (!rsp.Data!.IsPlaceholder)
        {
            var currentProfileId = profileId;
            var otherProfileId = rsp.Data!.OtherParticipant.ProfileId;

            await _hubContext.Clients
                .Group($"profile:{otherProfileId}")
                .SendAsync("NewConversation", rsp.Data);

            await _hubContext.Clients
                .Group($"profile:{currentProfileId}")
                .SendAsync("NewConversation", rsp.Data);
        }

        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [HttpGet("api/chats")]
    [EndpointSummary("Listar conversaciones")]
    [EndpointDescription("Obtiene las conversaciones del usuario autenticado.")]
    [ProducesResponseType<GenericResponse<List<ChatDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<List<ChatDto>>> GetConversations()
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _chatService.GetConversationsAsync(profileId);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [HttpGet("api/chats/{conversationId}/messages")]
    [EndpointSummary("Obtener mensajes")]
    [EndpointDescription("Obtiene los mensajes de una conversación.")]
    [ProducesResponseType<GenericResponse<PagedResult<MessageResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<PagedResult<MessageResponse>>> GetMessages(
        Guid conversationId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _chatService.GetMessagesAsync(profileId, conversationId, page, Math.Clamp(pageSize, 1, 100));
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [HttpPost("api/chats/{conversationId}/messages")]
    [EndpointSummary("Enviar mensaje")]
    [EndpointDescription("Envía un mensaje en una conversación.")]
    [ProducesResponseType<GenericResponse<MessageResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<MessageResponse>> SendMessage(Guid conversationId, [FromBody] SendMessageRequest request)
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _chatService.SendMessageAsync(profileId, conversationId, request.Content);

        var msg = rsp.Data!;
        var senderInfo = await _chatService.GetProfileInfoAsync(msg.SenderProfileId);

        var broadcast = new ChatMessageBroadcast(
            msg.Id,
            msg.ConversationId,
            msg.Content,
            msg.IsSeen,
            msg.IsEdited,
            msg.EditedAt,
            msg.CreatedAt,
            msg.DeletedAt,
            senderInfo!
        );

        await _hubContext.Clients
            .Group($"conversation:{conversationId}")
            .SendAsync("ReceiveMessage", broadcast);

        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [HttpDelete("api/chats/{conversationId}/messages/{messageId}")]
    [EndpointSummary("Eliminar mensaje")]
    [EndpointDescription("Elimina un mensaje específico de una conversación.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<string>> DeleteMessage(Guid conversationId, Guid messageId)
    {
        var profileId = GetProfileId() ?? throw new UnauthorizedException(ResponseMessages.InvalidToken);
        var rsp = await _chatService.DeleteMessageAsync(profileId, conversationId, messageId);
        return ResponseStatus.Ok(HttpContext, rsp);
    }
}
