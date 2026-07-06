using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class CommunityMemberRepository(OrbitDbContext context)
    : GenericRepository<CommunityMember>(context), ICommunityMemberRepository
{
    public async Task<List<CommunityMember>> GetListAsync(Expression<Func<CommunityMember, bool>> predicate)
    {
        return await DbSet.Where(predicate).ToListAsync();
    }
}
