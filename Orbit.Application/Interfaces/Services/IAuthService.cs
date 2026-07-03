using Orbit.Application.Common;
using Orbit.Application.Models.DTOs;

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

    Task<Result<AuthResponse>> LoginAsync(string emailOrUsername, string password);
    Task<Result<AuthResponse>> RefreshTokenAsync(string accessToken, string refreshToken);
    Task<Result> LogoutAsync(string refreshToken);
    Task<Result<ProfileResponse>> GetCurrentUserAsync(Guid authUserId);
    Task<Result> ForgotPasswordAsync(string emailOrUsername);
    Task<Result> ResetPasswordAsync(string username, string token, string newPassword);
}
