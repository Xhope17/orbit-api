using Orbit.Domain.Common;

namespace Orbit.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
}
