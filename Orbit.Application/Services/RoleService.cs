using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.Entities;
using Orbit.Domain.DataBase;

namespace Orbit.Application.Services;

public class RoleService : IRoleService
{
    private readonly IUnitOfWork _uow;

    public RoleService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result> AssignModeratorAsync(Guid adminProfileId, string targetUsername)
    {
        var slug = targetUsername.ToLowerInvariant();
        var target = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (target is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        if (target.Id == adminProfileId)
            return Result.Failure("Cannot assign moderator role to yourself");

        var moderatorRole = await _uow.roleRepository.Get(r => r.Name == "moderator");
        if (moderatorRole is null)
            return Result.Failure("Moderator role not found");

        var existing = await _uow.userRoleRepository.Get(ur =>
            ur.ProfileId == target.Id && ur.RoleId == moderatorRole.Id);
        if (existing is not null)
            return Result.Failure(ResponseMessages.UserAlreadyModerator);

        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            ProfileId = target.Id,
            RoleId = moderatorRole.Id,
            AssignedAt = DateTime.UtcNow,
        };

        await _uow.userRoleRepository.Create(userRole);
        await _uow.SaveChangesAsync();
        return Result.Success(ResponseMessages.RoleAssigned);
    }

    public async Task<Result> RemoveModeratorAsync(Guid adminProfileId, string targetUsername)
    {
        var slug = targetUsername.ToLowerInvariant();
        var target = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (target is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        var moderatorRole = await _uow.roleRepository.Get(r => r.Name == "moderator");
        if (moderatorRole is null)
            return Result.Failure("Moderator role not found");

        var existing = await _uow.userRoleRepository.Get(ur =>
            ur.ProfileId == target.Id && ur.RoleId == moderatorRole.Id);
        if (existing is null)
            return Result.Failure(ResponseMessages.UserNotModerator);

        await _uow.userRoleRepository.Delete(existing);
        await _uow.SaveChangesAsync();
        return Result.Success(ResponseMessages.RoleRemoved);
    }
}
