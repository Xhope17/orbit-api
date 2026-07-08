using Orbit.Application.Common;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Models.Responses.Auth;

namespace Orbit.Application.Interfaces.Services;

public interface IAuthService
{
    Task<Result<RegisterResponse>> RegisterAsync(
        string email,
        string username,
        string displayName,
        string password,
        Stream? profilePictureStream,
        string? profilePictureFileName,
        string? bio);

    Task<Result<LoginAuthResponse>> LoginAsync(string emailOrUsername, string password);
    Task<Result<LoginAuthResponse>> RefreshTokenAsync(string accessToken, string refreshToken);
    Task<Result> LogoutAsync(string refreshToken);
    Task<Result<ProfileDto>> GetCurrentUserAsync(Guid authUserId);
    Task<Result> ForgotPasswordAsync(string emailOrUsername);
    Task<Result> ResetPasswordAsync(string username, string token, string newPassword);
}
