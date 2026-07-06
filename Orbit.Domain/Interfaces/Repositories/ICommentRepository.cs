using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface ICommentRepository : IGenericRepository<Comment>
{
    Task<List<Comment>> GetListAsync(Expression<Func<Comment, bool>> predicate);
    Task<int> CountAsync(Expression<Func<Comment, bool>> predicate);
    Task<List<Comment>> GetPagedAsync<TKey>(
        Expression<Func<Comment, bool>> predicate,
        Expression<Func<Comment, TKey>> orderByDescending,
        int skip,
        int take);
}
