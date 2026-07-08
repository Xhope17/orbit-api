using Orbit.Application.Constants;
using Orbit.Application.Helpers;
using Orbit.Application.Interfaces.Services;
using Orbit.Application.Models.Responses;
using Orbit.Domain.Entities;
using Orbit.Domain.DataBase;
using Orbit.Domain.Exceptions;

namespace Orbit.Application.Services;

public class RoleService : IRoleService
{
    private readonly IUnitOfWork _uow;

    public RoleService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<GenericResponse<string>> AssignModeratorAsync(Guid adminProfileId, string targetUsername)
    {
        var slug = targetUsername.ToLowerInvariant();
        var target = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (target is null)
            throw new NotFoundException(ResponseMessages.ProfileNotFound);

        if (target.Id == adminProfileId)
            throw new BadRequestException("Cannot assign moderator role to yourself");

        var moderatorRole = await _uow.roleRepository.Get(r => r.Name == "moderator");
        if (moderatorRole is null)
            throw new NotFoundException("Moderator role not found");

        var existing = await _uow.userRoleRepository.Get(ur =>
            ur.ProfileId == target.Id && ur.RoleId == moderatorRole.Id);
        if (existing is not null)
            throw new BadRequestException(ResponseMessages.UserAlreadyModerator);

        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            ProfileId = target.Id,
            RoleId = moderatorRole.Id,
            AssignedAt = DateTime.UtcNow,
        };

        await _uow.userRoleRepository.Create(userRole);
        await _uow.SaveChangesAsync();
        return ResponseHelper.Create<string>(default, message: ResponseMessages.RoleAssigned);
    }

    public async Task<GenericResponse<string>> RemoveModeratorAsync(Guid adminProfileId, string targetUsername)
    {
        var slug = targetUsername.ToLowerInvariant();
        var target = await _uow.profileRepository.Get(p => p.UsernameSlug == slug);
        if (target is null)
            throw new NotFoundException(ResponseMessages.ProfileNotFound);

        var moderatorRole = await _uow.roleRepository.Get(r => r.Name == "moderator");
        if (moderatorRole is null)
            throw new NotFoundException("Moderator role not found");

        var existing = await _uow.userRoleRepository.Get(ur =>
            ur.ProfileId == target.Id && ur.RoleId == moderatorRole.Id);
        if (existing is null)
            throw new BadRequestException(ResponseMessages.UserNotModerator);

        await _uow.userRoleRepository.Delete(existing);
        await _uow.SaveChangesAsync();
        return ResponseHelper.Create<string>(default, message: ResponseMessages.RoleRemoved);
    }
}
