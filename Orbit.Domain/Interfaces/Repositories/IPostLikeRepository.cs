using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface IPostLikeRepository : IGenericRepository<PostLike>
{
    Task<List<PostLike>> GetListAsync(Expression<Func<PostLike, bool>> predicate);
}
