using Orbit.Application.Common;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.DataBase;
using Orbit.Domain.Entities;

namespace Orbit.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;

    public NotificationService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<PagedResult<NotificationResponse>>> GetNotificationsAsync(Guid profileId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        var notifications = await _uow.NotificationRepository.GetPagedAsync(
            n => n.ProfileId == profileId,
            n => n.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _uow.NotificationRepository.CountAsync(n => n.ProfileId == profileId);

        var actorProfileIds = notifications.Select(n => n.ActorProfileId).Distinct().ToList();
        var actorProfiles = actorProfileIds.Count > 0
            ? await _uow.ProfileRepository.GetListAsync(p => actorProfileIds.Contains(p.Id))
            : [];
        var actorMap = actorProfiles.ToDictionary(p => p.Id);

        var items = notifications.Select(n =>
        {
            var actor = actorMap.GetValueOrDefault(n.ActorProfileId);
            var actorResponse = actor is not null
                ? new PostAuthorResponse(actor.Id, actor.Username, actor.DisplayName, actor.ProfilePictureUrl, false)
                : new PostAuthorResponse(n.ActorProfileId, "Unknown", "Unknown", null, false);

            return new NotificationResponse(
                n.Id, n.Type, actorResponse, n.PostId, n.PostPreview,
                n.CommentId, n.CommentPreview, n.TotalCount, n.IsRead, n.CreatedAt);
        }).ToList();

        return Result<PagedResult<NotificationResponse>>.Success(new PagedResult<NotificationResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    public async Task<Result<int>> GetUnreadCountAsync(Guid profileId)
    {
        var count = await _uow.NotificationRepository.CountAsync(n => n.ProfileId == profileId && !n.IsRead);
        return Result<int>.Success(count);
    }

    public async Task<Result> MarkAsReadAsync(Guid profileId, Guid notificationId)
    {
        var notif = await _uow.NotificationRepository.Get(n => n.Id == notificationId && n.ProfileId == profileId);
        if (notif is null)
            return Result.Failure("Notification not found");

        notif.IsRead = true;
        notif.UpdatedAt = DateTime.UtcNow;
        await _uow.NotificationRepository.Update(notif);
        await _uow.SaveChangesAsync();
        return Result.Success("Notification marked as read");
    }

    public async Task<Result> MarkAllAsReadAsync(Guid profileId)
    {
        var unread = await _uow.NotificationRepository.GetListAsync(n => n.ProfileId == profileId && !n.IsRead);
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.UpdatedAt = DateTime.UtcNow;
            await _uow.NotificationRepository.Update(n);
        }
        await _uow.SaveChangesAsync();
        return Result.Success("All notifications marked as read");
    }
}
