using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class NotificationRepository(OrbitDbContext context)
    : GenericRepository<Notification>(context), INotificationRepository
{
    public async Task<List<Notification>> GetListAsync(Expression<Func<Notification, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }

    public async Task<int> CountAsync(Expression<Func<Notification, bool>> predicate)
    {
        return await DbSet.CountAsync(predicate);
    }

    public async Task<List<Notification>> GetPagedAsync<TKey>(
        Expression<Func<Notification, bool>> predicate,
        Expression<Func<Notification, TKey>> orderByDescending,
        int skip,
        int take)
    {
        return await DbSet
            .Where(predicate)
            .OrderByDescending(orderByDescending)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
}
