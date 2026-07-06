using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class PostMediaRepository(OrbitDbContext context)
    : GenericRepository<PostMedia>(context), IPostMediaRepository
{
    public async Task<List<PostMedia>> GetListAsync(Expression<Func<PostMedia, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }
}
