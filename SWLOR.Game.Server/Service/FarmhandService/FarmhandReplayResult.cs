using System.Collections.Generic;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.FarmhandService
{
    /// <summary>What a farmhand's replay did. The replay changes the records it was given in place; the caller saves them.</summary>
    public sealed class FarmhandReplayResult
    {
        /// <summary>Every event applied, in the order it was applied: by time, then by type.</summary>
        public List<FarmEvent> Events { get; } = new();

        /// <summary>The animals that went unfed for 72 hours. They are gone, and the caller deletes their records.</summary>
        public List<Livestock> WanderedOff { get; } = new();

        /// <summary>The crops that were left ripe for 72 hours. They are gone, and the caller clears their plots.</summary>
        public List<GardenPlot> Withered { get; } = new();

        /// <summary>The records the replay left standing, which now carry the replay's time as their last processed time.</summary>
        public List<Livestock> Animals { get; } = new();

        public List<GardenPlot> Plots { get; } = new();

        /// <summary>False for a replay of an empty span, which does nothing at all.</summary>
        public bool Ran { get; set; }
    }
}
