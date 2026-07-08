using Orbit.Application.Common;
using Orbit.Application.Models.DTOs;
using Orbit.Application.Models.Responses;

namespace Orbit.Application.Interfaces.Services;

public interface INotificationService
{
    Task<GenericResponse<PagedResult<NotificationDto>>> GetNotificationsAsync(Guid profileId, int page, int pageSize);
    Task<GenericResponse<int>> GetUnreadCountAsync(Guid profileId);
    Task<GenericResponse<string>> MarkAsReadAsync(Guid profileId, Guid notificationId);
    Task<GenericResponse<string>> MarkAllAsReadAsync(Guid profileId);
}
