using Orbit.Application.Models.Responses;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Models.Responses.Auth;

namespace Orbit.Application.Interfaces.Services;

public interface IAuthService
{
    Task<GenericResponse<RegisterResponse>> RegisterAsync(
        string email,
        string username,
        string displayName,
        string password,
        Stream? profilePictureStream,
        string? profilePictureFileName,
        string? bio);

    Task<GenericResponse<LoginAuthResponse>> LoginAsync(string emailOrUsername, string password);
    Task<GenericResponse<LoginAuthResponse>> RefreshTokenAsync(string accessToken, string refreshToken);
    Task<GenericResponse<string>> LogoutAsync(string refreshToken);
    Task<GenericResponse<ProfileDto>> GetCurrentUserAsync(Guid authUserId);
    Task<GenericResponse<string>> ForgotPasswordAsync(string emailOrUsername);
    Task<GenericResponse<string>> ResetPasswordAsync(string username, string token, string newPassword);
}
