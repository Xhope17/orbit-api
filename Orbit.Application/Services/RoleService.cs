using Orbit.Application.Common;
using Orbit.Application.Constants;
using Orbit.Application.Interfaces.Services;
using Orbit.Domain.Entities;
using Orbit.Domain.Interfaces.Repositories;

namespace Orbit.Application.Services;

public class RoleService : IRoleService
{
    private readonly IGenericRepository<Profile> _profileRepo;
    private readonly IGenericRepository<Role> _roleRepo;
    private readonly IGenericRepository<UserRole> _userRoleRepo;

    public RoleService(
        IGenericRepository<Profile> profileRepo,
        IGenericRepository<Role> roleRepo,
        IGenericRepository<UserRole> userRoleRepo)
    {
        _profileRepo = profileRepo;
        _roleRepo = roleRepo;
        _userRoleRepo = userRoleRepo;
    }

    public async Task<Result> AssignModeratorAsync(Guid adminProfileId, string targetUsername)
    {
        var slug = targetUsername.ToLowerInvariant();
        var target = await _profileRepo.FirstOrDefaultAsync(p => p.UsernameSlug == slug);
        if (target is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        if (target.Id == adminProfileId)
            return Result.Failure("Cannot assign moderator role to yourself");

        var moderatorRole = await _roleRepo.FirstOrDefaultAsync(r => r.Name == "moderator");
        if (moderatorRole is null)
            return Result.Failure("Moderator role not found");

        var existing = await _userRoleRepo.FirstOrDefaultAsync(ur =>
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

        await _userRoleRepo.CreateAsync(userRole);
        return Result.Success(ResponseMessages.RoleAssigned);
    }

    public async Task<Result> RemoveModeratorAsync(Guid adminProfileId, string targetUsername)
    {
        var slug = targetUsername.ToLowerInvariant();
        var target = await _profileRepo.FirstOrDefaultAsync(p => p.UsernameSlug == slug);
        if (target is null)
            return Result.Failure(ResponseMessages.ProfileNotFound);

        var moderatorRole = await _roleRepo.FirstOrDefaultAsync(r => r.Name == "moderator");
        if (moderatorRole is null)
            return Result.Failure("Moderator role not found");

        var existing = await _userRoleRepo.FirstOrDefaultAsync(ur =>
            ur.ProfileId == target.Id && ur.RoleId == moderatorRole.Id);
        if (existing is null)
            return Result.Failure(ResponseMessages.UserNotModerator);

        _userRoleRepo.Remove(existing);
        await _userRoleRepo.SaveChangesAsync();
        return Result.Success(ResponseMessages.RoleRemoved);
    }
}
