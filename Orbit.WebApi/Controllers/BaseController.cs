using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Orbit.Application.Constants;

namespace Orbit.WebApi.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected Guid? GetProfileId()
    {
        var claim = User.FindFirst(ClaimConstants.ProfileId)?.Value;
        if (claim is null || !Guid.TryParse(claim, out var profileId))
            return null;
        return profileId;
    }

    protected Guid? GetAuthUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst(ClaimConstants.Sub)?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var authUserId))
            return null;
        return authUserId;
    }
}
