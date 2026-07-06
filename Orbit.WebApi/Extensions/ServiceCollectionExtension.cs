using System.Text;
using CloudinaryDotNet;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Orbit.Application.Interfaces.Services;
using Orbit.Application.Services;
using Orbit.Domain.DataBase.Context;
using Orbit.Application.Interfaces.Repositories;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Infrastructure.Persistence.Database.Repositories;
using Orbit.Infrastructure.Services;
using Orbit.Shared.Constants;
using Orbit.WebApi.Middlewares;
using Orbit.WebApi.Validators;
using Orbit.WebApi.Workers;

namespace Orbit.WebApi.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static void AddInfrastructure(this IServiceCollection services)
        {
            var connectionString = Environment.GetEnvironmentVariable(EnvironmentConstants.DefaultConnection)
                ?? Environment.GetEnvironmentVariable(EnvironmentConstants.DefaultConnectionAlt);

            if (!string.IsNullOrEmpty(connectionString))
            {
                services.AddDbContext<OrbitDbContext>(options =>
                    options.UseNpgsql(connectionString));
            }

            services.AddCloudinary();
            services.AddHashing();
            services.AddJwt();
            services.AddRedis();
            services.AddEmail();
            services.AddRepositories();
            services.AddNotifications();
            services.AddApplicationServices();
            services.AddFluentValidation();
            services.AddMiddlleWares();
            services.AddControllers();
            services.AddSignalR();
        }

        public static IServiceCollection AddCloudinary(this IServiceCollection services)
        {
            var cloudName = Environment.GetEnvironmentVariable(EnvironmentConstants.CloudinaryCloudName) ?? string.Empty;
            var apiKey = Environment.GetEnvironmentVariable(EnvironmentConstants.CloudinaryApiKey) ?? string.Empty;
            var apiSecret = Environment.GetEnvironmentVariable(EnvironmentConstants.CloudinaryApiSecret) ?? string.Empty;

            var account = new Account(cloudName, apiKey, apiSecret);
            var cloudinary = new Cloudinary(account);

            services.AddSingleton(cloudinary);
            services.AddScoped<ICloudinaryService, CloudinaryService>();

            return services;
        }

        public static IServiceCollection AddHashing(this IServiceCollection services)
        {
            services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
            return services;
        }

        public static IServiceCollection AddJwt(this IServiceCollection services)
        {
            var jwtOptions = new JwtOptions
            {
                Secret = Environment.GetEnvironmentVariable(EnvironmentConstants.JwtSecret) ?? string.Empty,
                Issuer = Environment.GetEnvironmentVariable(EnvironmentConstants.JwtIssuer) ?? DefaultsConstants.JwtIssuer,
                Audience = Environment.GetEnvironmentVariable(EnvironmentConstants.JwtAudience) ?? DefaultsConstants.JwtAudience,
                AccessTokenExpirationMinutes = int.TryParse(
                    Environment.GetEnvironmentVariable(EnvironmentConstants.JwtAccessTokenExpiration), out var accessMin)
                    ? accessMin : DefaultsConstants.JwtAccessTokenExpirationMinutes,
                RefreshTokenExpirationDays = int.TryParse(
                    Environment.GetEnvironmentVariable(EnvironmentConstants.JwtRefreshTokenExpiration), out var refreshDays)
                    ? refreshDays : DefaultsConstants.JwtRefreshTokenExpirationDays,
            };

            services.AddSingleton(jwtOptions);
            services.AddScoped<IJwtService, JwtService>();

            return services;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IChatRepository, ChatRepository>();
            return services;
        }

        public static IServiceCollection AddRedis(this IServiceCollection services)
        {
            var connection = Environment.GetEnvironmentVariable(EnvironmentConstants.RedisConnection)
                ?? DefaultsConstants.RedisConnection;

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connection;
            });

            services.AddScoped<IResetTokenService, ResetTokenService>();

            return services;
        }

        public static IServiceCollection AddNotifications(this IServiceCollection services)
        {
            services.AddSingleton<NotificationChannel>();
            services.AddScoped<INotificationService, NotificationService>();
            return services;
        }

        public static IServiceCollection AddEmail(this IServiceCollection services)
        {
            var mailOptions = new MailOptions
            {
                Host = Environment.GetEnvironmentVariable(EnvironmentConstants.SmtpHost) ?? DefaultsConstants.SmtpHost,
                Port = int.TryParse(Environment.GetEnvironmentVariable(EnvironmentConstants.SmtpPort), out var port)
                    ? port : DefaultsConstants.SmtpPort,
                Username = Environment.GetEnvironmentVariable(EnvironmentConstants.SmtpUsername) ?? string.Empty,
                Password = Environment.GetEnvironmentVariable(EnvironmentConstants.SmtpPassword) ?? string.Empty,
                ApiKey = Environment.GetEnvironmentVariable(EnvironmentConstants.BrevoApiKey) ?? string.Empty,
                FromName = Environment.GetEnvironmentVariable(EnvironmentConstants.SmtpFromName) ?? DefaultsConstants.SmtpFromName,
                FromEmail = Environment.GetEnvironmentVariable(EnvironmentConstants.SmtpFromEmail) ?? DefaultsConstants.SmtpFromEmail,
            };

            services.AddSingleton(mailOptions);
            services.AddHttpClient<IEmailService, EmailService>();

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<IFollowService, FollowService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<ICommunityService, CommunityService>();
            services.AddScoped<IHashtagService, HashtagService>();
            services.AddHostedService<NotificationBackgroundService>();
            return services;
        }

        public static IServiceCollection AddFluentValidation(this IServiceCollection services)
        {
            services.AddValidatorsFromAssemblyContaining<RegisterValidator>();
            return services;
        }

        public static IServiceCollection AddMiddlleWares(this IServiceCollection services)
        {
            services.AddScoped<ErrorHandlerMiddleware>();
            return services;
        }

        public static IServiceCollection AddAuth(this IServiceCollection services)
        {
            var jwtSecret = Environment.GetEnvironmentVariable(EnvironmentConstants.JwtSecret) ?? string.Empty;
            var jwtIssuer = Environment.GetEnvironmentVariable(EnvironmentConstants.JwtIssuer) ?? DefaultsConstants.JwtIssuer;
            var jwtAudience = Environment.GetEnvironmentVariable(EnvironmentConstants.JwtAudience) ?? DefaultsConstants.JwtAudience;

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSecret)),
                    };
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("admin"));

                options.AddPolicy("ModeratorOrAdmin", policy =>
                    policy.RequireAssertion(context =>
                        context.User.IsInRole("moderator") || context.User.IsInRole("admin")));
            });

            return services;
        }
    }
}
