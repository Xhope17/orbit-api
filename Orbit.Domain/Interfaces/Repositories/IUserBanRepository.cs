using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface IUserBanRepository : IGenericRepository<UserBan>
{
    Task<List<UserBan>> GetListAsync(Expression<Func<UserBan, bool>> predicate);
    Task<int> CountAsync(Expression<Func<UserBan, bool>> predicate);
    Task<List<UserBan>> GetPagedAsync<TKey>(
        Expression<Func<UserBan, bool>> predicate,
        Expression<Func<UserBan, TKey>> orderByDescending,
        int skip,
        int take);
}
