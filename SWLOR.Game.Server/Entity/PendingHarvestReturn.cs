using System.Collections.Generic;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// Harvest units a Prospecting action paid out that the saved character file may not hold yet. Kept until the file is known to hold
    /// them; a login whose file predates <see cref="Sequence"/> gives them again. See <c>HarvestReturnJournal</c>.
    /// </summary>
    public class PendingHarvestReturn
    {
        /// <summary>The craft file sequence the payout was made at. A character file whose sequence is at least this holds the units.</summary>
        public int Sequence { get; set; }

        /// <summary>The units paid out, by resref.</summary>
        public Dictionary<string, int> Items { get; set; } = new();
    }
}
