using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// The incursion schedule and the running incursion (plan section 30), in one record. The next start is saved so a
    /// restart neither skips nor repeats a week, and the running incursion resumes. A new type, so no migration.
    /// </summary>
    public class IncursionState : EntityBase
    {
        public const string RecordId = "SPACE_INCURSIONS";

        public IncursionState()
        {
            Id = RecordId;
            LastCenter = string.Empty;
            PendingPayouts = new Dictionary<string, IncursionPayout>();
            PendingBattlePayouts = new Dictionary<string, IncursionPayout>();
            PendingRiftPayouts = new Dictionary<string, IncursionPayout>();
        }

        /// <summary>When the next weekly incursion starts.</summary>
        public DateTime NextStartAt { get; set; }

        /// <summary>The centre of the last incursion's region, never picked twice in a row.</summary>
        public string LastCenter { get; set; }

        /// <summary>The running incursion, or null.</summary>
        public ActiveIncursion Active { get; set; }

        /// <summary>Player id to the payout of a player who was offline when the region was cleared.</summary>
        public Dictionary<string, IncursionPayout> PendingPayouts { get; set; }

        /// <summary>
        /// Player id to the capital battle spoils of a player who was offline when the battle group was destroyed (W11p).
        /// A new property on an existing record: a record saved before it reads as null, so callers create it on use.
        /// </summary>
        public Dictionary<string, IncursionPayout> PendingBattlePayouts { get; set; }

        /// <summary>
        /// Player id to the Rift Run spoils of a cleared pocket, saved before any are handed out so a restart mid-payout
        /// leaves the rest to collect at the next login. A new property on an existing record: a record saved before it
        /// reads as null, so callers create it on use.
        /// </summary>
        public Dictionary<string, IncursionPayout> PendingRiftPayouts { get; set; }

        /// <summary>
        /// Key user id to the Rift Run spoils (player id to payout) of a pocket that key user opened and left before a save
        /// held the spent key. Their next login releases them into <see cref="PendingRiftPayouts"/> when the saved file holds
        /// the key as spent, or drops them when it returns the key. Older records read as null.
        /// </summary>
        public Dictionary<string, Dictionary<string, IncursionPayout>> HeldRiftPayouts { get; set; }
    }
}
