using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A running incursion (plan section 30): its region, invader and window, each area's progress, its blockades and the
    /// players' contributions. Saved in the incursion record, so a restart resumes it.
    /// </summary>
    public class ActiveIncursion
    {
        public string Center { get; set; } = string.Empty;
        public IncursionInvader Invader { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime EndsAt { get; set; }

        /// <summary>Area resref to its progress, one per region area.</summary>
        public Dictionary<string, IncursionAreaProgress> Areas { get; set; } = new();

        /// <summary>Every blockaded dock (see <see cref="IncursionRules.DockKey"/>) to the planet type it serves.</summary>
        public Dictionary<string, int> Blockades { get; set; } = new();

        public List<string> BrokenBlockades { get; set; } = new();

        /// <summary>
        /// The blockaded player station docks (W11o): dock key to the station's city id. Their <see cref="Blockades"/> entry
        /// serves no planet (0), so they close only the station's own Trade Exchange. Empty on incursions saved before.
        /// </summary>
        public Dictionary<string, string> StationBlockades { get; set; } = new();

        /// <summary>Player id to contribution.</summary>
        public Dictionary<string, IncursionContribution> Contributions { get; set; } = new();
    }
}
