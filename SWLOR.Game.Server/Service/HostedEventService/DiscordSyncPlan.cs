using System.Collections.Generic;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.HostedEventService
{
    /// <summary>
    /// What one Discord sync changes in the event list.
    /// </summary>
    public class DiscordSyncPlan
    {
        // Discord events not yet listed in game.
        public List<DiscordScheduledEvent> Create { get; } = new();
        // Imported events a DM has not started whose details changed on Discord.
        public List<(HostedEvent Event, DiscordScheduledEvent Discord)> Update { get; } = new();
        // Imported events a DM has not started that Discord no longer lists (cancelled, deleted or finished there).
        public List<HostedEvent> Remove { get; } = new();
        // Dismissals whose Discord event is gone or has moved to another start time.
        public List<string> DropDismissals { get; } = new();
    }
}
