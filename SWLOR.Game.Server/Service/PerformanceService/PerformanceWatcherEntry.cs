using System.Collections.Generic;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>One watcher found in a show during a tick, with the performers whose range covers them.</summary>
    public sealed class PerformanceWatcherEntry
    {
        public PerformanceWatcherEntry(uint player, Vector3 position, List<PerformerSnapshot> candidates)
        {
            Player = player;
            Position = position;
            Candidates = candidates;
        }

        public uint Player { get; }
        public Vector3 Position { get; }
        public List<PerformerSnapshot> Candidates { get; }
        public PerformanceAudienceState State { get; set; }
        public PerformerSnapshot Dancer { get; set; }
        public PerformerSnapshot Musician { get; set; }
    }
}
