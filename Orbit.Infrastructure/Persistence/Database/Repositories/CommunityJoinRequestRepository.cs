using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class CommunityJoinRequestRepository(OrbitDbContext context)
    : GenericRepository<CommunityJoinRequest>(context), ICommunityJoinRequestRepository
{
}
