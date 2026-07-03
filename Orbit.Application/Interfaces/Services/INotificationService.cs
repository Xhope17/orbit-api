using Orbit.Application.Common;
using Orbit.Application.Models.DTOs;

namespace Orbit.Application.Interfaces.Services;

public interface INotificationService
{
    Task<Result<PagedResult<NotificationResponse>>> GetNotificationsAsync(Guid profileId, int page, int pageSize);
    Task<Result<int>> GetUnreadCountAsync(Guid profileId);
    Task<Result> MarkAsReadAsync(Guid profileId, Guid notificationId);
    Task<Result> MarkAllAsReadAsync(Guid profileId);
}
