using Orbit.Application.Common;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;

namespace Orbit.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IGenericRepository<Notification> _notifRepo;
    private readonly IGenericRepository<Profile> _profileRepo;

    public NotificationService(
        IGenericRepository<Notification> notifRepo,
        IGenericRepository<Profile> profileRepo)
    {
        _notifRepo = notifRepo;
        _profileRepo = profileRepo;
    }

    public async Task<Result<PagedResult<NotificationResponse>>> GetNotificationsAsync(Guid profileId, int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        var notifications = await _notifRepo.GetPagedAsync(
            n => n.ProfileId == profileId,
            n => n.CreatedAt,
            skip,
            pageSize);

        var totalCount = await _notifRepo.CountAsync(n => n.ProfileId == profileId);

        var actorProfileIds = notifications.Select(n => n.ActorProfileId).Distinct().ToList();
        var actorProfiles = actorProfileIds.Count > 0
            ? await _profileRepo.GetListAsync(p => actorProfileIds.Contains(p.Id))
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
        var count = await _notifRepo.CountAsync(n => n.ProfileId == profileId && !n.IsRead);
        return Result<int>.Success(count);
    }

    public async Task<Result> MarkAsReadAsync(Guid profileId, Guid notificationId)
    {
        var notif = await _notifRepo.FirstOrDefaultAsync(n => n.Id == notificationId && n.ProfileId == profileId);
        if (notif is null)
            return Result.Failure("Notification not found");

        notif.IsRead = true;
        notif.UpdatedAt = DateTime.UtcNow;
        _notifRepo.Update(notif);
        await _notifRepo.SaveChangesAsync();
        return Result.Success("Notification marked as read");
    }

    public async Task<Result> MarkAllAsReadAsync(Guid profileId)
    {
        var unread = await _notifRepo.GetListAsync(n => n.ProfileId == profileId && !n.IsRead);
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.UpdatedAt = DateTime.UtcNow;
            _notifRepo.Update(n);
        }
        await _notifRepo.SaveChangesAsync();
        return Result.Success("All notifications marked as read");
    }
}
