using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.MasteryService
{
    /// <summary>
    /// A character's progress in a single mastery. Keyed by Mastery.Id inside
    /// PlayerMasteryProfile.Masteries.
    /// </summary>
    public class PlayerMasteryLevel
    {
        public PlayerMasteryLevel()
        {
            TierHistory = new List<MasteryTierRecord>();
        }

        /// <summary>The character's current tier (1-5) in this mastery.</summary>
        public int Tier { get; set; }

        /// <summary>The UTC date the character first earned tier 1 of this mastery.</summary>
        public DateTime DateFirstEarned { get; set; }

        /// <summary>Every tier ever earned in this mastery, oldest first.</summary>
        public List<MasteryTierRecord> TierHistory { get; set; }
    }
}
