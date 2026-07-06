using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class PostHashtagRepository(OrbitDbContext context)
    : GenericRepository<PostHashtag>(context), IPostHashtagRepository
{
    public async Task<List<PostHashtag>> GetListAsync(Expression<Func<PostHashtag, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }
}
