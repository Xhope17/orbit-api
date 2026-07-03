using System.Threading.Channels;

namespace Orbit.Application.Services;

public class NotificationChannel
{
    public Channel<NotificationEvent> Channel { get; } = System.Threading.Channels.Channel.CreateUnbounded<NotificationEvent>();
}
