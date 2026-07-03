using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbit.WebApi.Models;
using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Services;

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
    [ProducesResponseType<Result<RegisterResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromForm] RegisterRequest request)
    {
        var validationResult = await _registerValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
            return BadRequest(new { isSuccess = false, message = ResponseMessages.ValidationFailed, errors });
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
                ResponseMessages.EmailAlreadyRegistered => Conflict(new { isSuccess = false, message = result.Message }),
                ResponseMessages.UsernameAlreadyTaken => Conflict(new { isSuccess = false, message = result.Message }),
                _ => StatusCode(500, new { isSuccess = false, message = result.Message }),
            };
        }

        return CreatedAtAction(nameof(Register), new { isSuccess = true, message = result.Message, data = result.Data });
    }

    [HttpPost("login")]
    [EndpointSummary("Inicio de sesión")]
    [EndpointDescription("Autentica al usuario con email/username y contraseña. Devuelve tokens de acceso y refresco.")]
    [ProducesResponseType<Result<AuthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var validationResult = await _loginValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
            return BadRequest(new { isSuccess = false, message = ResponseMessages.ValidationFailed, errors });
        }

        var result = await _authService.LoginAsync(request.EmailOrUsername, request.Password);

        if (!result.IsSuccess)
        {
            return Unauthorized(new { isSuccess = false, message = result.Message });
        }

        return Ok(new { isSuccess = true, message = result.Message, data = result.Data });
    }

    [HttpPost("logout")]
    [EndpointSummary("Cerrar sesión")]
    [EndpointDescription("Invalida el refresh token y cierra la sesión del usuario.")]
    [ProducesResponseType<Result>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var result = await _authService.LogoutAsync(request.RefreshToken);
        return Ok(new { isSuccess = true, message = result.Message });
    }

    [Authorize]
    [HttpGet("me")]
    [EndpointSummary("Usuario actual")]
    [EndpointDescription("Obtiene la información del usuario autenticado mediante el token JWT.")]
    [ProducesResponseType<Result<ProfileResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<Result>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst(ClaimConstants.Sub)?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var authUserId))
            return Unauthorized(new { isSuccess = false, message = ResponseMessages.InvalidToken });

        var result = await _authService.GetCurrentUserAsync(authUserId);

        if (!result.IsSuccess)
            return NotFound(new { isSuccess = false, message = result.Message });

        return Ok(new { isSuccess = true, data = result.Data });
    }

    [HttpPost("refresh")]
    [EndpointSummary("Refrescar token")]
    [EndpointDescription("Obtiene un nuevo access token usando el refresh token.")]
    [ProducesResponseType<Result<AuthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.AccessToken, request.RefreshToken);

        if (!result.IsSuccess)
        {
            return result.Message switch
            {
                ResponseMessages.SessionExpired => Unauthorized(new { isSuccess = false, message = result.Message }),
                _ => Unauthorized(new { isSuccess = false, message = result.Message }),
            };
        }

        return Ok(new { isSuccess = true, message = result.Message, data = result.Data });
    }

    [HttpPost("forgot-password")]
    [EndpointSummary("Olvidé mi contraseña")]
    [EndpointDescription("Envía un correo con el token para restablecer la contraseña.")]
    [ProducesResponseType<Result>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var validationResult = await _forgotPasswordValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
            return BadRequest(new { isSuccess = false, message = ResponseMessages.ValidationFailed, errors });
        }

        var result = await _authService.ForgotPasswordAsync(request.EmailOrUsername);
        return Ok(new { isSuccess = true, message = result.Message });
    }

    [HttpPost("reset-password")]
    [EndpointSummary("Restablecer contraseña")]
    [EndpointDescription("Restablece la contraseña usando el token recibido por correo.")]
    [ProducesResponseType<Result>(StatusCodes.Status200OK)]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var validationResult = await _resetPasswordValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
            return BadRequest(new { isSuccess = false, message = ResponseMessages.ValidationFailed, errors });
        }

        var result = await _authService.ResetPasswordAsync(request.Username, request.Token, request.NewPassword);

        if (!result.IsSuccess)
            return BadRequest(new { isSuccess = false, message = result.Message });

        return Ok(new { isSuccess = true, message = result.Message });
    }
}
