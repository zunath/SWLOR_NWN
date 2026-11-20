using System.Collections.Generic;
using SWLOR.Game.Server.Service.HostedEventService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A DM-hosted event players find with /events. Ended events are deleted.
    /// </summary>
    public class HostedEvent : EntityBase
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string HostName { get; set; }
        public string CreatedBy { get; set; }
        public HostedEventStatus Status { get; set; }
        public DateTime ScheduledStartUtc { get; set; }
        public DateTime? StartedUtc { get; set; }
        public bool IsJoinOpen { get; set; }
        public bool AnnounceOnStart { get; set; }

        // The Discord Events tab entry this event was listed from, or empty for an event made in game.
        public string DiscordEventId { get; set; }
        // Discord's start time for the occurrence this event was listed from. Kept when a DM edits the start time
        // after starting the event, so ending it hides that occurrence.
        public DateTime? DiscordStartUtc { get; set; }

        // Empty when the DM has not set an arrival point; players cannot join until one is set.
        public string ArrivalAreaResref { get; set; }
        public float ArrivalX { get; set; }
        public float ArrivalY { get; set; }
        public float ArrivalZ { get; set; }
        public float ArrivalFacing { get; set; }

        // Players who joined and have not left. They keep their way back if the event ends first.
        public HashSet<string> ParticipantPlayerIds { get; set; }

        /// <summary>
        /// A copy to change and save before it replaces this event in the cache.
        /// </summary>
        public HostedEvent Copy()
        {
            var copy = (HostedEvent)MemberwiseClone();
            copy.ParticipantPlayerIds = new HashSet<string>(ParticipantPlayerIds ?? new HashSet<string>());
            return copy;
        }

        public HostedEvent()
        {
            Name = string.Empty;
            Description = string.Empty;
            HostName = string.Empty;
            CreatedBy = string.Empty;
            Status = HostedEventStatus.Scheduled;
            ArrivalAreaResref = string.Empty;
            DiscordEventId = string.Empty;
            AnnounceOnStart = true;
            IsJoinOpen = true;
            ParticipantPlayerIds = new HashSet<string>();
        }
    }
}
