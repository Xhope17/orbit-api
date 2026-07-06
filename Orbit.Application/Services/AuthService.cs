using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Enums;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.DataBase;
using Orbit.Domain.Entities;
using Orbit.Shared.Constants;

namespace Orbit.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly IResetTokenService _resetTokenService;

    private const string TokenChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public AuthService(
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        ICloudinaryService cloudinaryService,
        IJwtService jwtService,
        IEmailService emailService,
        IResetTokenService resetTokenService)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
        _cloudinaryService = cloudinaryService;
        _jwtService = jwtService;
        _emailService = emailService;
        _resetTokenService = resetTokenService;
    }

    private static string GenerateResetToken()
    {
        return string.Create(6, TokenChars, (span, chars) =>
        {
            for (int i = 0; i < 6; i++)
                span[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
        });
    }

    public async Task<Result<RegisterResponse>> RegisterAsync(
        string email,
        string username,
        string displayName,
        string password,
        Stream? profilePictureStream,
        string? profilePictureFileName,
        string? bio)
    {
        var usernameSlug = username.ToLowerInvariant();

        var emailExists = await _uow.authUserRepository.Get(u => u.Email == email);
        if (emailExists is not null)
            return Result<RegisterResponse>.Failure(ResponseMessages.EmailAlreadyRegistered);

        var usernameExists = await _uow.profileRepository.Get(p => p.UsernameSlug == usernameSlug);
        if (usernameExists is not null)
            return Result<RegisterResponse>.Failure(ResponseMessages.UsernameAlreadyTaken);

        var passwordHash = _passwordHasher.Hash(password);
        var authUser = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _uow.authUserRepository.Create(authUser);

        string? avatarUrl = null;

        if (profilePictureStream is not null && !string.IsNullOrEmpty(profilePictureFileName))
        {
            var fileName = $"{authUser.Id}_{Guid.NewGuid()}";
            var uploadResult = await _cloudinaryService.UploadAsync(
                profilePictureStream, fileName, CloudinaryFolder.ProfilePics);

            if (uploadResult.IsSuccess)
            {
                avatarUrl = uploadResult.Data!.Url;
            }
        }

        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            AuthUserId = authUser.Id,
            Username = username,
            UsernameSlug = usernameSlug,
            DisplayName = displayName,
            Bio = bio,
            ProfilePictureUrl = avatarUrl,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _uow.profileRepository.Create(profile);

        var userRole = await _uow.roleRepository.Get(r => r.Name == "user");
        if (userRole is not null)
        {
            var userRoleAssignment = new UserRole
            {
                Id = Guid.NewGuid(),
                ProfileId = profile.Id,
                RoleId = userRole.Id,
                AssignedAt = DateTime.UtcNow,
            };
            await _uow.userRoleRepository.Create(userRoleAssignment);
        }

        await _uow.SaveChangesAsync();

        await SendWelcomeEmailAsync(email, displayName, username);

        return Result<RegisterResponse>.Success(new RegisterResponse(
            authUser.Id, email, username, displayName, avatarUrl, bio
        ), ResponseMessages.RegistrationSuccessful);
    }

    public async Task<Result<AuthResponse>> LoginAsync(string emailOrUsername, string password)
    {
        var authUser = await _uow.authUserRepository.Get(u => u.Email == emailOrUsername);

        if (authUser is null)
        {
            var profileByUsername = await _uow.profileRepository.Get(p => p.Username == emailOrUsername);
            if (profileByUsername is not null)
                authUser = await _uow.authUserRepository.Get(u => u.Id == profileByUsername.AuthUserId);
        }

        if (authUser is null)
            return Result<AuthResponse>.Failure(ResponseMessages.InvalidCredentials);

        if (!_passwordHasher.Verify(password, authUser.PasswordHash))
            return Result<AuthResponse>.Failure(ResponseMessages.InvalidCredentials);

        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUser.Id);
        if (profile is null)
            return Result<AuthResponse>.Failure(ResponseMessages.InvalidCredentials);

        if (profile.IsBanned)
            return Result<AuthResponse>.Failure(ResponseMessages.AccountBanned);

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);

        var roles = await GetUserRolesAsync(profile.Id);
        var (accessToken, expiresAt) = _jwtService.GenerateAccessToken(authUser.Id, profile.Id, profile.Username, roles);

        var rawRefreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenHash = _passwordHasher.Hash(rawRefreshToken);
        var tokenKey = ComputeTokenKey(rawRefreshToken);

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            AuthUserId = authUser.Id,
            RefreshTokenHash = refreshTokenHash,
            TokenKey = tokenKey,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
        };

        await _uow.userSessionRepository.Create(session);
        await _uow.SaveChangesAsync();

        var profileResponse = BuildProfileResponse(profile, prefixResponse);
        var response = new AuthResponse(accessToken, rawRefreshToken, expiresAt, profileResponse, roles);
        return Result<AuthResponse>.Success(response, ResponseMessages.LoginSuccessful);
    }

    public async Task<Result> LogoutAsync(string refreshToken)
    {
        var tokenKey = ComputeTokenKey(refreshToken);
        var session = await _uow.userSessionRepository.Get(s => s.TokenKey == tokenKey);

        if (session is not null)
        {
            await _uow.userSessionRepository.Delete(session);
            await _uow.SaveChangesAsync();
        }

        return Result.Success(ResponseMessages.LoggedOutSuccessfully);
    }

    public async Task<Result<ProfileResponse>> GetCurrentUserAsync(Guid authUserId)
    {
        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result<ProfileResponse>.Failure(ResponseMessages.ProfileNotFound);

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);
        var profileResponse = BuildProfileResponse(profile, prefixResponse);

        return Result<ProfileResponse>.Success(profileResponse);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(string accessToken, string refreshToken)
    {
        var principal = _jwtService.GetPrincipalFromExpiredToken(accessToken);
        if (principal is null)
            return Result<AuthResponse>.Failure(ResponseMessages.InvalidOrExpiredToken);

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? principal.FindFirst(ClaimConstants.Sub)?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var authUserId))
            return Result<AuthResponse>.Failure(ResponseMessages.InvalidOrExpiredToken);

        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            return Result<AuthResponse>.Failure(ResponseMessages.InvalidOrExpiredToken);

        var tokenKey = ComputeTokenKey(refreshToken);
        var validSession = await _uow.userSessionRepository.Get(s =>
            s.TokenKey == tokenKey && s.AuthUserId == authUserId);

        if (validSession is null)
            return Result<AuthResponse>.Failure(ResponseMessages.InvalidRefreshToken);

        if (validSession.ExpiresAt < DateTime.UtcNow)
            return Result<AuthResponse>.Failure(ResponseMessages.SessionExpired);

        await _uow.userSessionRepository.Delete(validSession);

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);

        var roles = await GetUserRolesAsync(profile.Id);
        var (newAccessToken, expiresAt) = _jwtService.GenerateAccessToken(authUserId, profile.Id, profile.Username, roles);

        var rawRefreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenHash = _passwordHasher.Hash(rawRefreshToken);
        var newTokenKey = ComputeTokenKey(rawRefreshToken);

        var newSession = new UserSession
        {
            Id = Guid.NewGuid(),
            AuthUserId = authUserId,
            RefreshTokenHash = refreshTokenHash,
            TokenKey = newTokenKey,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
        };

        await _uow.userSessionRepository.Create(newSession);
        await _uow.SaveChangesAsync();

        var profileResponse = BuildProfileResponse(profile, prefixResponse);
        var response = new AuthResponse(newAccessToken, rawRefreshToken, expiresAt, profileResponse, roles);
        return Result<AuthResponse>.Success(response, ResponseMessages.TokenRefreshed);
    }

    public async Task<Result> ForgotPasswordAsync(string emailOrUsername)
    {
        var normalizedInput = emailOrUsername.ToLowerInvariant();
        var authUser = await _uow.authUserRepository.Get(u => u.Email == normalizedInput);

        if (authUser is null)
        {
            var profileByUsername = await _uow.profileRepository.Get(p => p.UsernameSlug == normalizedInput);
            if (profileByUsername is not null)
                authUser = await _uow.authUserRepository.Get(u => u.Id == profileByUsername.AuthUserId);
        }

        if (authUser is not null)
        {
            var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUser.Id);
            if (profile is null)
                return Result.Success(ResponseMessages.CheckYourInbox);

            var toName = profile.DisplayName;
            var usernameSlug = profile.UsernameSlug;

            var token = GenerateResetToken();
            await _resetTokenService.SaveTokenAsync(usernameSlug, token, TimeSpan.FromMinutes(15));

            var frontendUrl = Environment.GetEnvironmentVariable(EnvironmentConstants.FrontendUrl) ?? DefaultsConstants.FrontendUrl;
            var resetUrl = $"{frontendUrl}/reset-password?username={Uri.EscapeDataString(usernameSlug)}&token={Uri.EscapeDataString(token)}";

            var template = await _uow.emailTemplateRepository.Get(t => t.Name == "password-reset");
            if (template is not null)
            {
                var htmlBody = template.HtmlBody
                    .Replace("{{displayName}}", toName)
                    .Replace("{{username}}", profile.Username)
                    .Replace("{{resetUrl}}", resetUrl);

                var subject = template.Subject
                    .Replace("{{displayName}}", toName);

                await _emailService.SendAsync(authUser.Email, toName, subject, htmlBody);
            }
        }

        return Result.Success(ResponseMessages.CheckYourInbox);
    }

    public async Task<Result> ResetPasswordAsync(string username, string token, string newPassword)
    {
        var usernameSlug = username.ToLowerInvariant();
        var storedToken = await _resetTokenService.GetTokenAsync(usernameSlug);

        if (storedToken is null || storedToken != token)
            return Result.Failure(ResponseMessages.InvalidOrExpiredToken);

        var profile = await _uow.profileRepository.Get(p => p.UsernameSlug == usernameSlug);
        if (profile is null)
            return Result.Failure(ResponseMessages.InvalidOrExpiredToken);

        var authUser = await _uow.authUserRepository.Get(u => u.Id == profile.AuthUserId);
        if (authUser is null)
            return Result.Failure(ResponseMessages.InvalidOrExpiredToken);

        authUser.PasswordHash = _passwordHasher.Hash(newPassword);
        authUser.UpdatedAt = DateTime.UtcNow;
        await _uow.authUserRepository.Update(authUser);
        await _uow.SaveChangesAsync();

        await _resetTokenService.RemoveTokenAsync(usernameSlug);

        return Result.Success(ResponseMessages.PasswordResetSuccessful);
    }

    private async Task SendWelcomeEmailAsync(string email, string displayName, string username)
    {
        var template = await _uow.emailTemplateRepository.Get(t => t.Name == "welcome");
        if (template is null) return;

        var frontendUrl = Environment.GetEnvironmentVariable(EnvironmentConstants.FrontendUrl) ?? DefaultsConstants.FrontendUrl;

        var htmlBody = template.HtmlBody
            .Replace("{{displayName}}", displayName)
            .Replace("{{username}}", username)
            .Replace("{{frontendUrl}}", frontendUrl);

        var subject = template.Subject
            .Replace("{{displayName}}", displayName);

        await _emailService.SendAsync(email, displayName, subject, htmlBody);
    }

    private async Task<List<string>> GetUserRolesAsync(Guid profileId)
    {
        var userRoles = await _uow.userRoleRepository.GetListAsync(ur => ur.ProfileId == profileId);
        if (userRoles.Count == 0) return [];

        var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
        var roles = await _uow.roleRepository.GetListAsync(r => roleIds.Contains(r.Id));
        return roles.Select(r => r.Name).ToList();
    }

    private static string ComputeTokenKey(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexStringLower(bytes);
    }

    private async Task<UserPrefixResponse?> GetPrefixAsync(Guid? prefixId)
    {
        if (!prefixId.HasValue) return null;

        var prefix = await _uow.userPrefixRepository.Get(p => p.Id == prefixId.Value);
        return prefix is null ? null : new UserPrefixResponse(prefix.Id, prefix.Name, prefix.Color, prefix.IconUrl);
    }

    private static ProfileResponse BuildProfileResponse(Profile profile, UserPrefixResponse? prefix)
    {
        return new ProfileResponse(
            profile.Id,
            profile.Username,
            profile.DisplayName,
            profile.ProfilePictureUrl,
            profile.BannerUrl,
            profile.Bio,
            profile.FollowersCount,
            profile.FollowingCount,
            profile.IsVerified,
            prefix,
            false
        );
    }
}
