using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class CommentLikeRepository(OrbitDbContext context)
    : GenericRepository<CommentLike>(context), ICommentLikeRepository
{
    public async Task<List<CommentLike>> GetListAsync(Expression<Func<CommentLike, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }
}
