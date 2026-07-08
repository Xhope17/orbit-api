using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbit.WebApi.Models;
using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Helpers;
using Orbit.Application.Models.Responses;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Models.Responses.Auth;
using Orbit.Application.Interfaces.Services;
using Orbit.WebApi.Helpers;

namespace Orbit.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<ForgotPasswordRequest> forgotPasswordValidator,
        IValidator<ResetPasswordRequest> resetPasswordValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
    }

    [HttpPost("register")]
    [EndpointSummary("Registro de usuario")]
    [EndpointDescription("Crea una nueva cuenta con email, username, display name y contraseña. Opcionalmente se puede subir una foto de perfil y biografía.")]
    [ProducesResponseType<GenericResponse<RegisterResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status409Conflict)]
    public async Task<GenericResponse<RegisterResponse>> Register([FromForm] RegisterRequest request)
    {
        var validationResult = await _registerValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<RegisterResponse>(default, errors: [.. errors], message: ResponseMessages.ValidationFailed));
        }

        Stream? fileStream = null;
        if (request.ProfilePicture is not null)
        {
            fileStream = request.ProfilePicture.OpenReadStream();
        }

        var result = await _authService.RegisterAsync(
            request.Email,
            request.Username,
            request.DisplayName,
            request.Password,
            fileStream,
            request.ProfilePicture?.FileName,
            request.Bio
        );

        if (!result.IsSuccess)
        {
            return result.Message switch
            {
                ResponseMessages.EmailAlreadyRegistered => ResponseStatus.Conflict(HttpContext, ResponseHelper.Create<RegisterResponse>(default, message: result.Message)),
                ResponseMessages.UsernameAlreadyTaken => ResponseStatus.Conflict(HttpContext, ResponseHelper.Create<RegisterResponse>(default, message: result.Message)),
                _ => ResponseStatus.InternalServerError(HttpContext, ResponseHelper.Create<RegisterResponse>(default, message: result.Message)),
            };
        }

        return ResponseStatus.Created(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }

    [HttpPost("login")]
    [EndpointSummary("Inicio de sesión")]
    [EndpointDescription("Autentica al usuario con email/username y contraseña. Devuelve tokens de acceso y refresco.")]
    [ProducesResponseType<GenericResponse<LoginAuthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<LoginAuthResponse>> Login([FromBody] LoginRequest request)
    {
        var validationResult = await _loginValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<LoginAuthResponse>(default, errors: [.. errors], message: ResponseMessages.ValidationFailed));
        }

        var result = await _authService.LoginAsync(request.EmailOrUsername, request.Password);

        if (!result.IsSuccess)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<LoginAuthResponse>(default, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }

    [HttpPost("logout")]
    [EndpointSummary("Cerrar sesión")]
    [EndpointDescription("Invalida el refresh token y cierra la sesión del usuario.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    public async Task<GenericResponse<string>> Logout([FromBody] LogoutRequest request)
    {
        var result = await _authService.LogoutAsync(request.RefreshToken);
        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }

    [Authorize]
    [HttpGet("me")]
    [EndpointSummary("Usuario actual")]
    [EndpointDescription("Obtiene la información del usuario autenticado mediante el token JWT.")]
    [ProducesResponseType<GenericResponse<ProfileDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<GenericResponse<ProfileDto>> Me()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst(ClaimConstants.Sub)?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var authUserId))
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<ProfileDto>(default, message: ResponseMessages.InvalidToken));

        var result = await _authService.GetCurrentUserAsync(authUserId);

        if (!result.IsSuccess)
            return ResponseStatus.NotFound(HttpContext, ResponseHelper.Create<ProfileDto>(default, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }

    [HttpPost("refresh")]
    [EndpointSummary("Refrescar token")]
    [EndpointDescription("Obtiene un nuevo access token usando el refresh token.")]
    [ProducesResponseType<GenericResponse<LoginAuthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<LoginAuthResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.AccessToken, request.RefreshToken);

        if (!result.IsSuccess)
            return ResponseStatus.Unauthorized(HttpContext, ResponseHelper.Create<LoginAuthResponse>(default, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create(data: result.Data!, message: result.Message));
    }

    [HttpPost("forgot-password")]
    [EndpointSummary("Olvidé mi contraseña")]
    [EndpointDescription("Envía un correo con el token para restablecer la contraseña.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    public async Task<GenericResponse<string>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var validationResult = await _forgotPasswordValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, errors: [.. errors], message: ResponseMessages.ValidationFailed));
        }

        var result = await _authService.ForgotPasswordAsync(request.EmailOrUsername);
        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }

    [HttpPost("reset-password")]
    [EndpointSummary("Restablecer contraseña")]
    [EndpointDescription("Restablece la contraseña usando el token recibido por correo.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    public async Task<GenericResponse<string>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var validationResult = await _resetPasswordValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, errors: [.. errors], message: ResponseMessages.ValidationFailed));
        }

        var result = await _authService.ResetPasswordAsync(request.Username, request.Token, request.NewPassword);

        if (!result.IsSuccess)
            return ResponseStatus.BadRequest(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));

        return ResponseStatus.Ok(HttpContext, ResponseHelper.Create<string>(null, message: result.Message));
    }
}
