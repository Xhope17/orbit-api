using System.Linq.Expressions;
using Orbit.Domain.Entities;

namespace Orbit.Domain.Interfaces.Repositories;

public interface ICommentLikeRepository : IGenericRepository<CommentLike>
{
    Task<List<CommentLike>> GetListAsync(Expression<Func<CommentLike, bool>> predicate);
}
