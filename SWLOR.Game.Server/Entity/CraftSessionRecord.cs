using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// The materials a craft in progress has taken from a player. It is saved on the player so a logout, death or server
    /// restart can settle the attempt as a failure instead of losing the materials with no result.
    /// </summary>
    public class CraftSessionRecord
    {
        public string SessionId { get; set; }
        public RecipeType Recipe { get; set; }
        public SkillType Skill { get; set; }
        public bool HasBlueprint { get; set; }
        public int BlueprintLevel { get; set; }
        public DateTime StartedUtc { get; set; }

        /// <summary>
        /// The actions the craft has resolved, saved after each one, so a craft settled with no window (a logout, a death, a restart, a
        /// replacement) is held to the same XP pace as one settled by its window. A craft with none earns no failure XP.
        /// </summary>
        public int ActionsTaken { get; set; }

        /// <summary>
        /// The craft file sequence the debit was made at. A character file below it still holds the components, enhancements and blueprint
        /// charge, so a login whose file is that old discards the session without returning anything. Zero (records saved before this
        /// field) means the file is trusted to be current.
        /// </summary>
        public int DebitSequence { get; set; }

        /// <summary>
        /// The station fee the craft took from the player with its debit (a Shipyard's fee for a Capital hull, space revamp), or 0. The
        /// station's treasury is credited when the craft ends, never at the start, so a crash that rolls the payment back credits nothing.
        /// </summary>
        public int StationFee { get; set; }

        /// <summary>The station property whose treasury takes <see cref="StationFee"/>.</summary>
        public string StationFeePropertyId { get; set; }

        /// <summary>One serialized item per committed component stack.</summary>
        public List<string> Components { get; set; } = new();

        /// <summary>The serialized enhancement in each slot; index 0 is slot 1, and an empty slot is an empty string.</summary>
        public List<string> Enhancements { get; set; } = new();
    }
}
