using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>The performers and watchers of one area during a tick.</summary>
    public sealed class PerformanceAreaShow
    {
        public PerformanceAreaShow(uint area)
        {
            Area = area;
        }

        public uint Area { get; }
        public List<PerformerSnapshot> Performers { get; } = new();
        public List<PerformanceWatcherEntry> Watchers { get; } = new();

        /// <summary>Public CD keys already counted as a watcher in this show; a second character on the same key does not count.</summary>
        public HashSet<string> CdKeys { get; } = new();
    }
}
