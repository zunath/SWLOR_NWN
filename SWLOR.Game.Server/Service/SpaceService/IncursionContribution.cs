using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>One player's tracked contribution to a running incursion. Saved with the incursion.</summary>
    public class IncursionContribution
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Contribution points by the tier of the area they were earned in.</summary>
        public Dictionary<int, double> Points { get; set; } = new();

        public int Kills { get; set; }
        public long Damage { get; set; }
        public long Repairs { get; set; }

        public double TotalPoints() => Points.Values.Sum();
    }
}
