using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Orbit.Application.Constants;
using Orbit.Application.Helpers;
using Orbit.Application.Models.Responses;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Models.Responses.Auth;
using Orbit.Application.Enums;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.DataBase;
using Orbit.Domain.Entities;
using Orbit.Domain.Exceptions;
using Orbit.Shared.Constants;

namespace Orbit.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly IResetTokenService _resetTokenService;

    private const string TokenChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public AuthService(
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        ICloudinaryService cloudinaryService,
        IConfiguration configuration,
        IEmailService emailService,
        IResetTokenService resetTokenService)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
        _cloudinaryService = cloudinaryService;
        _configuration = configuration;
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

    public async Task<GenericResponse<RegisterResponse>> RegisterAsync(
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
            throw new BadRequestException(ResponseMessages.EmailAlreadyRegistered);

        var usernameExists = await _uow.profileRepository.Get(p => p.UsernameSlug == usernameSlug);
        if (usernameExists is not null)
            throw new BadRequestException(ResponseMessages.UsernameAlreadyTaken);

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

        return ResponseHelper.Create(new RegisterResponse(
            authUser.Id, email, username, displayName, avatarUrl, bio
        ), message: ResponseMessages.RegistrationSuccessful);
    }

    public async Task<GenericResponse<LoginAuthResponse>> LoginAsync(string emailOrUsername, string password)
    {
        var authUser = await _uow.authUserRepository.Get(u => u.Email == emailOrUsername);

        if (authUser is null)
        {
            var profileByUsername = await _uow.profileRepository.Get(p => p.Username == emailOrUsername);
            if (profileByUsername is not null)
                authUser = await _uow.authUserRepository.Get(u => u.Id == profileByUsername.AuthUserId);
        }

        if (authUser is null)
            throw new BadRequestException(ResponseMessages.InvalidCredentials);

        if (!_passwordHasher.Verify(password, authUser.PasswordHash))
            throw new BadRequestException(ResponseMessages.InvalidCredentials);

        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUser.Id);
        if (profile is null)
            throw new BadRequestException(ResponseMessages.InvalidCredentials);

        if (profile.IsBanned)
            throw new BadRequestException(ResponseMessages.AccountBanned);

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);

        var roles = await GetUserRolesAsync(profile.Id);
        var tokenConfig = TokenHelper.Configuration(_configuration);
        var accessToken = TokenHelper.Create(authUser.Id, profile.Id, profile.Username, roles, tokenConfig);

        var (rawRefreshToken, session) = TokenHelper.CreateSession(authUser.Id, _passwordHasher);

        await _uow.userSessionRepository.Create(session);
        await _uow.SaveChangesAsync();

        var profileResponse = BuildProfileDto(profile, prefixResponse);
        var response = new LoginAuthResponse(accessToken, rawRefreshToken, tokenConfig.Expiration, profileResponse, roles);
        return ResponseHelper.Create(response, message: ResponseMessages.LoginSuccessful);
    }

    public async Task<GenericResponse<string>> LogoutAsync(string refreshToken)
    {
        var tokenKey = TokenHelper.ComputeTokenKey(refreshToken);
        var session = await _uow.userSessionRepository.Get(s => s.TokenKey == tokenKey);

        if (session is not null)
        {
            await _uow.userSessionRepository.Delete(session);
            await _uow.SaveChangesAsync();
        }

        return ResponseHelper.Create(string.Empty, message: ResponseMessages.LoggedOutSuccessfully);
    }

    public async Task<GenericResponse<ProfileDto>> GetCurrentUserAsync(Guid authUserId)
    {
        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            throw new NotFoundException(ResponseMessages.ProfileNotFound);

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);
        var profileResponse = BuildProfileDto(profile, prefixResponse);

        return ResponseHelper.Create(profileResponse);
    }

    public async Task<GenericResponse<LoginAuthResponse>> RefreshTokenAsync(string accessToken, string refreshToken)
    {
        var tokenConfig = TokenHelper.Configuration(_configuration);
        var principal = TokenHelper.GetPrincipalFromExpiredToken(accessToken, tokenConfig);
        if (principal is null)
            throw new BadRequestException(ResponseMessages.InvalidOrExpiredToken);

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? principal.FindFirst(ClaimConstants.Sub)?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var authUserId))
            throw new BadRequestException(ResponseMessages.InvalidOrExpiredToken);

        var profile = await _uow.profileRepository.Get(p => p.AuthUserId == authUserId);
        if (profile is null)
            throw new BadRequestException(ResponseMessages.InvalidOrExpiredToken);

        var tokenKey = TokenHelper.ComputeTokenKey(refreshToken);
        var validSession = await _uow.userSessionRepository.Get(s =>
            s.TokenKey == tokenKey && s.AuthUserId == authUserId);

        if (validSession is null)
            throw new BadRequestException(ResponseMessages.InvalidRefreshToken);

        if (validSession.ExpiresAt < DateTime.UtcNow)
            throw new BadRequestException(ResponseMessages.SessionExpired);

        await _uow.userSessionRepository.Delete(validSession);

        var prefixResponse = await GetPrefixAsync(profile.PrefixId);

        var roles = await GetUserRolesAsync(profile.Id);
        var newAccessToken = TokenHelper.Create(authUserId, profile.Id, profile.Username, roles, tokenConfig);

        var (rawRefreshToken, newSession) = TokenHelper.CreateSession(authUserId, _passwordHasher);

        await _uow.userSessionRepository.Create(newSession);
        await _uow.SaveChangesAsync();

        var profileResponse = BuildProfileDto(profile, prefixResponse);
        var response = new LoginAuthResponse(newAccessToken, rawRefreshToken, tokenConfig.Expiration, profileResponse, roles);
        return ResponseHelper.Create(response, message: ResponseMessages.TokenRefreshed);
    }

    public async Task<GenericResponse<string>> ForgotPasswordAsync(string emailOrUsername)
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
                return ResponseHelper.Create(string.Empty, message: ResponseMessages.CheckYourInbox);

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

        return ResponseHelper.Create(string.Empty, message: ResponseMessages.CheckYourInbox);
    }

    public async Task<GenericResponse<string>> ResetPasswordAsync(string username, string token, string newPassword)
    {
        var usernameSlug = username.ToLowerInvariant();
        var storedToken = await _resetTokenService.GetTokenAsync(usernameSlug);

        if (storedToken is null || storedToken != token)
            throw new BadRequestException(ResponseMessages.InvalidOrExpiredToken);

        var profile = await _uow.profileRepository.Get(p => p.UsernameSlug == usernameSlug);
        if (profile is null)
            throw new BadRequestException(ResponseMessages.InvalidOrExpiredToken);

        var authUser = await _uow.authUserRepository.Get(u => u.Id == profile.AuthUserId);
        if (authUser is null)
            throw new BadRequestException(ResponseMessages.InvalidOrExpiredToken);

        authUser.PasswordHash = _passwordHasher.Hash(newPassword);
        authUser.UpdatedAt = DateTime.UtcNow;
        await _uow.authUserRepository.Update(authUser);
        await _uow.SaveChangesAsync();

        await _resetTokenService.RemoveTokenAsync(usernameSlug);

        return ResponseHelper.Create(string.Empty, message: ResponseMessages.PasswordResetSuccessful);
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

    private async Task<UserPrefixDto?> GetPrefixAsync(Guid? prefixId)
    {
        if (!prefixId.HasValue) return null;

        var prefix = await _uow.userPrefixRepository.Get(p => p.Id == prefixId.Value);
        return prefix is null ? null : new UserPrefixDto(prefix.Id, prefix.Name, prefix.Color, prefix.IconUrl);
    }

    private static ProfileDto BuildProfileDto(Profile profile, UserPrefixDto? prefix)
    {
        return new ProfileDto(
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
