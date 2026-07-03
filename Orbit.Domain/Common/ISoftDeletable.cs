namespace Orbit.Domain.Common;

public interface ISoftDeletable
{
    bool IsActive { get; set; }
}
