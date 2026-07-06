using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface INotificationRepository : IGenericRepository<Notification>
{
    Task<List<Notification>> GetListAsync(Expression<Func<Notification, bool>> predicate);
    Task<int> CountAsync(Expression<Func<Notification, bool>> predicate);
    Task<List<Notification>> GetPagedAsync<TKey>(
        Expression<Func<Notification, bool>> predicate,
        Expression<Func<Notification, TKey>> orderByDescending,
        int skip,
        int take);
}
