using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbit.WebApi.Models;
using Orbit.Application.Constants;
using Orbit.Application.Models.Responses;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Models.Responses.Auth;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.Exceptions;
using Orbit.WebApi.Helpers;

namespace Orbit.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(
        IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [EndpointSummary("Registro de usuario")]
    [EndpointDescription("Crea una nueva cuenta con email, username, display name y contraseña. Opcionalmente se puede subir una foto de perfil y biografía.")]
    [ProducesResponseType<GenericResponse<RegisterResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status409Conflict)]
    public async Task<GenericResponse<RegisterResponse>> Register([FromForm] RegisterRequest request)
    {
        Stream? fileStream = null;
        if (request.ProfilePicture is not null)
        {
            fileStream = request.ProfilePicture.OpenReadStream();
        }

        var rsp = await _authService.RegisterAsync(
            request.Email,
            request.Username,
            request.DisplayName,
            request.Password,
            fileStream,
            request.ProfilePicture?.FileName,
            request.Bio
        );

        return ResponseStatus.Created(HttpContext, rsp);
    }

    [HttpPost("login")]
    [EndpointSummary("Inicio de sesión")]
    [EndpointDescription("Autentica al usuario con email/username y contraseña. Devuelve tokens de acceso y refresco.")]
    [ProducesResponseType<GenericResponse<LoginAuthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<LoginAuthResponse>> Login([FromBody] LoginRequest request)
    {
        var rsp = await _authService.LoginAsync(request.EmailOrUsername, request.Password);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [HttpPost("logout")]
    [EndpointSummary("Cerrar sesión")]
    [EndpointDescription("Invalida el refresh token y cierra la sesión del usuario.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    public async Task<GenericResponse<string>> Logout([FromBody] LogoutRequest request)
    {
        var rsp = await _authService.LogoutAsync(request.RefreshToken);
        return ResponseStatus.Ok(HttpContext, rsp);
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
            throw new UnauthorizedException(ResponseMessages.InvalidToken);

        var rsp = await _authService.GetCurrentUserAsync(authUserId);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [HttpPost("refresh")]
    [EndpointSummary("Refrescar token")]
    [EndpointDescription("Obtiene un nuevo access token usando el refresh token.")]
    [ProducesResponseType<GenericResponse<LoginAuthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status401Unauthorized)]
    public async Task<GenericResponse<LoginAuthResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var rsp = await _authService.RefreshTokenAsync(request.AccessToken, request.RefreshToken);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [HttpPost("forgot-password")]
    [EndpointSummary("Olvidé mi contraseña")]
    [EndpointDescription("Envía un correo con el token para restablecer la contraseña.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    public async Task<GenericResponse<string>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var rsp = await _authService.ForgotPasswordAsync(request.EmailOrUsername);
        return ResponseStatus.Ok(HttpContext, rsp);
    }

    [HttpPost("reset-password")]
    [EndpointSummary("Restablecer contraseña")]
    [EndpointDescription("Restablece la contraseña usando el token recibido por correo.")]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<GenericResponse<string>>(StatusCodes.Status400BadRequest)]
    public async Task<GenericResponse<string>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var rsp = await _authService.ResetPasswordAsync(request.Username, request.Token, request.NewPassword);
        return ResponseStatus.Ok(HttpContext, rsp);
    }
}
