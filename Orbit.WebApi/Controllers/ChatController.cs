using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Orbit.WebApi.Models;
using Orbit.WebApi.Hubs;
using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Helpers;
using Orbit.Application.Models.Responses;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Services;
using Orbit.WebApi.Helpers;

namespace Orbit.WebApi.Controllers;

[Authorize]
public class ChatController : BaseController
{
    private readonly IChatService _chatService;
    private readonly IValidator<CreateChatRequest> _createChatValidator;
    private readonly IValidator<SendMessageRequest> _sendMessageValidator;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatController(
        IChatService chatService,
        IValidator<CreateChatRequest> createChatValidator,
        IValidator<SendMessageRequest> sendMessageValidator,
        IHubContext<ChatHub> hubContext)
    {
        _chatService = chatService;
        _createChatValidator = createChatValidator;
        _sendMessageValidator = sendMessageValidator;
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
        var validationResult = await _createChatValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<ChatDto>(default, errors: validationResult.Errors.Select(e => e.ErrorMessage).ToList(), message: ResponseMessages.ValidationFailed, isSuccess: false));

        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<ChatDto>(default, message: ResponseMessages.InvalidToken, isSuccess: false));

        var result = await _chatService.CreateConversationAsync(profileId.Value, request.Username);
        if (!result.IsSuccess)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<ChatDto>(default, message: result.Message, isSuccess: false));

        if (!result.Data!.IsPlaceholder)
        {
            var currentProfileId = profileId.Value;
            var otherProfileId = result.Data!.OtherParticipant.ProfileId;

            await _hubContext.Clients
                .Group($"profile:{otherProfileId}")
                .SendAsync("NewConversation", result.Data);

            await _hubContext.Clients
                .Group($"profile:{currentProfileId}")
                .SendAsync("NewConversation", result.Data);
        }

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data, message: result.Message));
    }

    [HttpGet("api/chats")]
    [EndpointSummary("Listar conversaciones")]
    [EndpointDescription("Obtiene las conversaciones del usuario autenticado.")]
    [ProducesResponseType<GenericResponse<List<ChatDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<List<ChatDto>>> GetConversations()
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<List<ChatDto>>(default, message: ResponseMessages.InvalidToken, isSuccess: false));

        var result = await _chatService.GetConversationsAsync(profileId.Value);

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
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
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<PagedResult<MessageResponse>>(default, message: ResponseMessages.InvalidToken, isSuccess: false));

        var result = await _chatService.GetMessagesAsync(
            profileId.Value, conversationId, page, Math.Clamp(pageSize, 1, 100));

        if (!result.IsSuccess)
            return ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<PagedResult<MessageResponse>>(default, message: result.Message, isSuccess: false));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }

    [HttpPost("api/chats/{conversationId}/messages")]
    [EndpointSummary("Enviar mensaje")]
    [EndpointDescription("Envía un mensaje en una conversación.")]
    [ProducesResponseType<GenericResponse<MessageResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<MessageResponse>> SendMessage(Guid conversationId, [FromBody] SendMessageRequest request)
    {
        var validationResult = await _sendMessageValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<MessageResponse>(default, errors: validationResult.Errors.Select(e => e.ErrorMessage).ToList(), message: ResponseMessages.ValidationFailed, isSuccess: false));

        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<MessageResponse>(default, message: ResponseMessages.InvalidToken, isSuccess: false));

        var result = await _chatService.SendMessageAsync(profileId.Value, conversationId, request.Content);
        if (!result.IsSuccess)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<MessageResponse>(default, message: result.Message, isSuccess: false));

        var msg = result.Data!;
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

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<MessageResponse>(data: result.Data!, message: result.Message));
    }

    [HttpDelete("api/chats/{conversationId}/messages/{messageId}")]
    [EndpointSummary("Eliminar mensaje")]
    [EndpointDescription("Elimina un mensaje específico de una conversación.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<string>> DeleteMessage(Guid conversationId, Guid messageId)
    {
        var profileId = GetProfileId();
        if (profileId is null)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<string>(null, message: ResponseMessages.InvalidToken, isSuccess: false));

        var result = await _chatService.DeleteMessageAsync(profileId.Value, conversationId, messageId);
        if (!result.IsSuccess)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message, isSuccess: false));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }
}
