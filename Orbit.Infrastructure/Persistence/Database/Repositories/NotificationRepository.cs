using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;
using Orbit.Domain.DataBase.Context;

namespace Orbit.Infrastructure.Persistence.Database.Repositories;

public class NotificationRepository(OrbitDbContext context)
    : GenericRepository<Notification>(context), INotificationRepository
{
}
