using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Orbit.Application.Constants;

namespace Orbit.WebApi.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private Guid GetProfileId()
    {
        var claim = Context.User?.FindFirst(ClaimConstants.ProfileId)?.Value;
        if (claim is null || !Guid.TryParse(claim, out var profileId))
            throw new HubException("User not authenticated");
        return profileId;
    }

    public override async Task OnConnectedAsync()
    {
        var profileId = GetProfileId();
        await Groups.AddToGroupAsync(Context.ConnectionId, $"notifications:{profileId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var profileId = GetProfileId();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"notifications:{profileId}");
        await base.OnDisconnectedAsync(exception);
    }
}
