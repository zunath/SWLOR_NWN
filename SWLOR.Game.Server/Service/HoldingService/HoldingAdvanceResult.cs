using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.HoldingService
{
    /// <summary>What happened while a holding caught up to now: its new log lines and whether it was lost.</summary>
    public class HoldingAdvanceResult
    {
        public int Days { get; set; }
        public List<string> Events { get; } = new();

        /// <summary>The upkeep owed reached the foreclosure limit; the holding is lost.</summary>
        public bool Foreclosed { get; set; }
    }
}
