using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Orbit.WebApi.Hubs;
using Orbit.Application.Interfaces.Services;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Services;
using Orbit.Domain.DataBase.Context;
using Orbit.Domain.Entities;

namespace Orbit.WebApi.Workers;

public class NotificationBackgroundService : BackgroundService
{
    private readonly NotificationChannel _notificationChannel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationBackgroundService(
        NotificationChannel notificationChannel,
        IServiceScopeFactory scopeFactory,
        IHubContext<NotificationHub> hubContext)
    {
        _notificationChannel = notificationChannel;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in _notificationChannel.Channel.Reader.ReadAllAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OrbitDbContext>();

            try
            {
                var existing = await dbContext.Notifications
                    .FirstOrDefaultAsync(n =>
                        n.ProfileId == evt.TargetProfileId &&
                        n.Type == evt.Type &&
                        n.PostId == evt.PostId &&
                        !n.IsRead, stoppingToken);

                Notification notification;
                if (existing is not null)
                {
                    existing.ActorProfileId = evt.ActorProfileId;
                    existing.PostPreview = evt.PostPreview;
                    existing.CommentPreview = evt.CommentPreview;
                    existing.CommentId = evt.CommentId;
                    existing.UpdatedAt = DateTime.UtcNow;
                    notification = existing;
                }
                else
                {
                    notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        ProfileId = evt.TargetProfileId,
                        ActorProfileId = evt.ActorProfileId,
                        Type = evt.Type,
                        PostId = evt.PostId,
                        CommentId = evt.CommentId,
                        PostPreview = evt.PostPreview,
                        CommentPreview = evt.CommentPreview,
                        TotalCount = 0,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };
                    dbContext.Notifications.Add(notification);
                }

                int totalCount = evt.Type switch
                {
                    "like" => (await dbContext.Posts
                        .Where(p => p.Id == evt.PostId)
                        .Select(p => p.LikeCount)
                        .FirstOrDefaultAsync(stoppingToken)),
                    "comment" => (await dbContext.Posts
                        .Where(p => p.Id == evt.PostId)
                        .Select(p => p.CommentCount)
                        .FirstOrDefaultAsync(stoppingToken)),
                    "repost" => await dbContext.Posts
                        .CountAsync(p => p.OriginalPostId == evt.PostId && p.IsRepost, stoppingToken),
                    "thread" => await dbContext.Posts
                        .CountAsync(p => p.OriginalPostId == evt.PostId && p.IsThread, stoppingToken),
                    _ => 0
                };
                notification.TotalCount = totalCount;
                await dbContext.SaveChangesAsync(stoppingToken);

                var actorProfile = await dbContext.Profiles
                    .Where(p => p.Id == evt.ActorProfileId)
                    .Select(p => new { p.Id, p.Username, p.DisplayName, p.ProfilePictureUrl })
                    .FirstOrDefaultAsync(stoppingToken);

                if (actorProfile is null) continue;

                var response = new NotificationDto(
                    notification.Id,
                    notification.Type,
                    new PostAuthorDto(
                        actorProfile.Id,
                        actorProfile.Username,
                        actorProfile.DisplayName,
                        actorProfile.ProfilePictureUrl,
                        false
                    ),
                    notification.PostId,
                    notification.PostPreview,
                    notification.CommentId,
                    notification.CommentPreview,
                    notification.TotalCount,
                    notification.IsRead,
                    notification.CreatedAt
                );

                await _hubContext.Clients
                    .Group($"notifications:{evt.TargetProfileId}")
                    .SendAsync("ReceiveNotification", response, stoppingToken);
            }
            catch
            {
            }
        }
    }
}
