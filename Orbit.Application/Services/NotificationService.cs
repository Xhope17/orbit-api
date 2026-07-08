using Orbit.Application.Common;
using Orbit.Application.Helpers;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Models.Responses;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.DataBase;
using Orbit.Domain.Entities;
using Orbit.Domain.Exceptions;

namespace Orbit.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;

    public NotificationService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<GenericResponse<PagedResult<NotificationDto>>> GetNotificationsAsync(Guid profileId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        var notifications = await _uow.notificationRepository.GetPagedAsync(
            n => n.ProfileId == profileId,
            n => n.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _uow.notificationRepository.CountAsync(n => n.ProfileId == profileId);

        var actorProfileIds = notifications.Select(n => n.ActorProfileId).Distinct().ToList();
        var actorProfiles = actorProfileIds.Count > 0
            ? await _uow.profileRepository.GetListAsync(p => actorProfileIds.Contains(p.Id))
            : [];
        var actorMap = actorProfiles.ToDictionary(p => p.Id);

        var items = notifications.Select(n =>
        {
            var actor = actorMap.GetValueOrDefault(n.ActorProfileId);
            var actorResponse = actor is not null
                ? new PostAuthorDto(actor.Id, actor.Username, actor.DisplayName, actor.ProfilePictureUrl, false)
                : new PostAuthorDto(n.ActorProfileId, "Unknown", "Unknown", null, false);

            return new NotificationDto(
                n.Id, n.Type, actorResponse, n.PostId, n.PostPreview,
                n.CommentId, n.CommentPreview, n.TotalCount, n.IsRead, n.CreatedAt);
        }).ToList();

        return ResponseHelper.Create(new PagedResult<NotificationDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }

    public async Task<GenericResponse<int>> GetUnreadCountAsync(Guid profileId)
    {
        var count = await _uow.notificationRepository.CountAsync(n => n.ProfileId == profileId && !n.IsRead);
        return ResponseHelper.Create(count);
    }

    public async Task<GenericResponse<string>> MarkAsReadAsync(Guid profileId, Guid notificationId)
    {
        var notif = await _uow.notificationRepository.Get(n => n.Id == notificationId && n.ProfileId == profileId);
        if (notif is null)
            throw new NotFoundException("Notification not found");

        notif.IsRead = true;
        notif.UpdatedAt = DateTime.UtcNow;
        await _uow.notificationRepository.Update(notif);
        await _uow.SaveChangesAsync();
        return ResponseHelper.Create<string>(default, message: "Notification marked as read");
    }

    public async Task<GenericResponse<string>> MarkAllAsReadAsync(Guid profileId)
    {
        var unread = await _uow.notificationRepository.GetListAsync(n => n.ProfileId == profileId && !n.IsRead);
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.UpdatedAt = DateTime.UtcNow;
            await _uow.notificationRepository.Update(n);
        }
        await _uow.SaveChangesAsync();
        return ResponseHelper.Create<string>(default, message: "All notifications marked as read");
    }
}
