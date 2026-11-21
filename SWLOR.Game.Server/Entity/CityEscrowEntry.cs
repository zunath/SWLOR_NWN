using System.Collections.Generic;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// Credits held out of a city's balances for a bounty, tournament, or project, kept on
    /// <see cref="WorldProperty.Escrow"/> under the id of whatever they were posted for. Each portion remembers which
    /// balance it came from, so a refund can return it there.
    /// </summary>
    public class CityEscrowEntry
    {
        public CityEscrowEntry()
        {
            PlayerFees = new Dictionary<string, long>();
        }

        /// <summary>The part that came from <see cref="WorldProperty.RestrictedTreasury"/>.</summary>
        public long Restricted { get; set; }

        /// <summary>The part that came from <see cref="WorldProperty.Treasury"/>.</summary>
        public long Unrestricted { get; set; }

        /// <summary>Entry fees paid by players, keyed by player id. They return to the players, never to the city.</summary>
        public Dictionary<string, long> PlayerFees { get; set; }
    }
}
