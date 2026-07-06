using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class PostLikeRepository(OrbitDbContext context)
    : GenericRepository<PostLike>(context), IPostLikeRepository
{
    public async Task<List<PostLike>> GetListAsync(Expression<Func<PostLike, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }
}
